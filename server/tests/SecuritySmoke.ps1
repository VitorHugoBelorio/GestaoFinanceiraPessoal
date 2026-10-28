param([string]$BaseUrl = 'http://localhost:5134/api')

$ErrorActionPreference = 'Stop'
$suffix = [guid]::NewGuid().ToString('N')
$emails = @("security-a-$suffix@example.invalid", "security-b-$suffix@example.invalid")
$sessions = @((New-Object Microsoft.PowerShell.Commands.WebRequestSession), (New-Object Microsoft.PowerShell.Commands.WebRequestSession))

function Assert($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}

function Get-CsrfHeaders($Session) {
    $csrf = Invoke-RestMethod -Uri "$BaseUrl/auth/csrf" -WebSession $Session
    return @{ 'X-CSRF-TOKEN' = $csrf.token }
}

function Send-Json($Session, $Method, $Path, $Body) {
    Invoke-RestMethod -Uri "$BaseUrl$Path" -Method $Method -WebSession $Session -ContentType 'application/json' -Headers (Get-CsrfHeaders $Session) -Body ($Body | ConvertTo-Json -Depth 8)
}

function Get-HttpStatus($Session, $Method, $Path, $Body = $null) {
    $arguments = @{ Uri = "$BaseUrl$Path"; Method = $Method; WebSession = $Session }
    if ($Method -ne 'Get') { $arguments.Headers = Get-CsrfHeaders $Session }
    if ($null -ne $Body) {
        $arguments.ContentType = 'application/json'
        $arguments.Body = $Body | ConvertTo-Json -Depth 8
    }
    try {
        Invoke-WebRequest @arguments | Out-Null
        return 200
    }
    catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        throw
    }
}

try {
    Assert ((Get-HttpStatus $sessions[0] 'Get' '/transactions') -eq 401) 'Dados acessiveis sem login.'

    for ($index = 0; $index -lt 2; $index++) {
        Send-Json $sessions[$index] 'Post' '/auth/register-and-onboard' @{
            account = @{ name = "Teste seguro $index"; email = $emails[$index]; password = 'Teste-Local-2026!' }
            setup = @{ initialCapital = 100; employmentType = 'Clt'; incomeSetupMode = 'Manual'; fixedMonthlyIncome = $null; incomeDayOfMonth = $null; incomeReminderDay = $null }
        } | Out-Null
    }

    $loginSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Send-Json $loginSession 'Post' '/auth/login' @{ email = $emails[0]; password = 'Teste-Local-2026!' } | Out-Null
    $sessions[0] = $loginSession
    $loggedInUser = Invoke-RestMethod -Uri "$BaseUrl/auth/me" -WebSession $sessions[0]
    Assert ($loggedInUser.email -eq $emails[0]) 'Login nao manteve a conta correta.'

    $authCookie = $sessions[0].Cookies.GetCookies([uri]$BaseUrl) | Where-Object { $_.Name -eq 'gfp.session' } | Select-Object -First 1
    Assert ($null -ne $authCookie -and $authCookie.Expires -eq [datetime]::MinValue -and $authCookie.HttpOnly) 'O cookie de login nao e de sessao ou nao e HttpOnly.'
    try {
        Invoke-WebRequest -Uri "$BaseUrl/auth/logout" -Method Post -WebSession $sessions[0] | Out-Null
        throw 'Uma alteracao sem token antifalsificacao foi aceita.'
    }
    catch {
        if (-not $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 400) { throw }
    }

    $category = Send-Json $sessions[0] 'Post' '/categories' @{ name = 'Privada'; type = 'Income' }
    $entry = Send-Json $sessions[0] 'Post' '/transactions' @{
        type = 'Income'; description = 'Receita privada'; amount = 50; date = '2026-10-01'
        status = 'Confirmed'; categoryId = $category.id; isRecurring = $false; isInstallment = $false
    }
    $movement = Send-Json $sessions[0] 'Post' '/investments/movements' @{
        type = 'Contribution'; amount = 10; date = '2026-10-01'; note = 'Movimento privado'
    }

    Assert ((Get-HttpStatus $sessions[1] 'Get' "/transactions/$($entry.id)") -eq 404) 'Conta B leu lancamento da conta A.'
    Assert ((Get-HttpStatus $sessions[1] 'Put' "/transactions/$($entry.id)" @{
        description = 'Alterado'; amount = 99; date = '2026-10-01'; categoryId = $null
    }) -eq 404) 'Conta B alterou lancamento da conta A.'
    Assert ((Get-HttpStatus $sessions[1] 'Delete' "/transactions/$($entry.id)") -eq 404) 'Conta B excluiu lancamento da conta A.'
    Assert ((Get-HttpStatus $sessions[1] 'Delete' "/investments/movements/$($movement.id)") -eq 404) 'Conta B excluiu investimento da conta A.'
    Assert ((Get-HttpStatus $sessions[1] 'Post' '/transactions' @{
        type = 'Income'; description = 'Etiqueta alheia'; amount = 1; date = '2026-10-01'
        status = 'Confirmed'; categoryId = $category.id; isRecurring = $false; isInstallment = $false
    }) -eq 400) 'Conta B usou etiqueta da conta A.'

    $bCategories = @(Invoke-RestMethod -Uri "$BaseUrl/categories" -WebSession $sessions[1])
    $bEntries = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=10" -WebSession $sessions[1])
    $bDashboard = Invoke-RestMethod -Uri "$BaseUrl/dashboard?year=2026&month=10" -WebSession $sessions[1]
    Assert (@($bCategories | Where-Object { $_.id -eq $category.id }).Count -eq 0) 'Etiqueta apareceu em outra conta.'
    Assert (@($bEntries | Where-Object { $_.id -eq $entry.id }).Count -eq 0) 'Lancamento apareceu em outra conta.'
    Assert ($bDashboard.monthlyIncome -eq 0 -and $bDashboard.investedBalance -eq 0) 'Resumo misturou dados de contas.'

    $aEntry = Invoke-RestMethod -Uri "$BaseUrl/transactions/$($entry.id)" -WebSession $sessions[0]
    Assert ($aEntry.amount -eq 50) 'Tentativas da conta B alteraram o lancamento da conta A.'
    Send-Json $sessions[1] 'Post' '/auth/logout' @{} | Out-Null
    Assert ((Get-HttpStatus $sessions[1] 'Get' '/auth/me') -eq 401) 'Logout nao encerrou a sessao.'
    Write-Output 'PASS: cookie de sessao, login/logout, CSRF, acesso anonimo e isolamento entre duas contas.'
}
finally {
    $config = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\appsettings.Development.json') -Raw | ConvertFrom-Json
    $connection = New-Object System.Data.SqlClient.SqlConnection $config.ConnectionStrings.DefaultConnection
    try {
        $connection.Open()
        $transaction = $connection.BeginTransaction()
        try {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = @'
DECLARE @UserId int = (SELECT Id FROM Users WHERE NormalizedEmail = @Email);
IF @UserId IS NOT NULL
BEGIN
    DELETE FROM Transactions WHERE UserId = @UserId;
    DELETE FROM RecurringTransactions WHERE UserId = @UserId;
    DELETE FROM MonthlySpendingPlans WHERE UserId = @UserId;
    DELETE FROM InvestmentMovements WHERE UserId = @UserId;
    DELETE FROM Categories WHERE UserId = @UserId;
    DELETE FROM FinancialProfiles WHERE UserId = @UserId;
    DELETE FROM Users WHERE Id = @UserId;
END
'@
            $parameter = $command.Parameters.Add('@Email', [System.Data.SqlDbType]::NVarChar, 256)
            foreach ($email in $emails) {
                $parameter.Value = $email.ToUpperInvariant()
                $command.ExecuteNonQuery() | Out-Null
            }
            $transaction.Commit()
        }
        catch { $transaction.Rollback(); throw }
    }
    finally { $connection.Dispose() }
}
