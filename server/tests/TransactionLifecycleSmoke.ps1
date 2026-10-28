param([string]$BaseUrl = 'http://localhost:5134/api')

$ErrorActionPreference = 'Stop'
$email = "transaction-smoke-$([guid]::NewGuid().ToString('N'))@example.invalid"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Send-Json($Method, $Path, $Body) {
    $json = $Body | ConvertTo-Json -Depth 8
    try {
        Invoke-RestMethod -Uri "$BaseUrl$Path" -Method $Method -WebSession $session -ContentType 'application/json' -Headers (Get-CsrfHeaders) -Body $json
    }
    catch {
        $response = $_.Exception.Response
        if ($response) {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            throw "Falha em $Path ($([int]$response.StatusCode)): $($reader.ReadToEnd())"
        }
        throw
    }
}

function Get-CsrfHeaders {
    $csrf = Invoke-RestMethod -Uri "$BaseUrl/auth/csrf" -WebSession $session
    return @{ 'X-CSRF-TOKEN' = $csrf.token }
}

function Assert($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}

try {
    Send-Json 'Post' '/auth/register-and-onboard' @{
        account = @{ name = 'Teste temporario'; email = $email; password = 'Teste-Local-2026!' }
        setup = @{ initialCapital = 0; employmentType = 'Clt'; incomeSetupMode = 'Manual'; fixedMonthlyIncome = $null; incomeDayOfMonth = $null; incomeReminderDay = $null }
    } | Out-Null

    $currentUser = Invoke-RestMethod -Uri "$BaseUrl/auth/me" -WebSession $session
    Assert ($currentUser.email -eq $email) 'A autenticacao nao foi mantida apos o cadastro.'

    $created = Send-Json 'Post' '/transactions' @{
        type = 'Expense'; description = 'Teste recorrencia'; note = 'Nota inicial'; amount = 100
        date = '2026-10-08'; status = 'Confirmed'; categoryId = $null; isRecurring = $true
        isInstallment = $true; totalPurchaseAmount = 600; recurrenceEndDate = '2027-03-31'
    }
    Assert ($created.id -gt 0) 'Falha ao criar a serie.'

    $december = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=12" -WebSession $session)
    Assert (@($december | Where-Object { $_.description -eq 'Teste recorrencia' }).Count -eq 1) 'Dezembro nao foi gerado.'
    $detail = Invoke-RestMethod -Uri "$BaseUrl/transactions/$($created.id)" -WebSession $session
    Assert ($detail.installmentCount -eq 6 -and $detail.recurrenceEndDate -eq '2027-03-31') 'Detalhes incompletos.'

    $edit = @{ description = 'Teste ajustado'; note = 'Nota alterada'; amount = 120; date = '2026-10-08'; categoryId = $null; recurrenceEndDate = '2026-11-30'; totalPurchaseAmount = 240 }
    Send-Json 'Put' "/transactions/$($created.id)" $edit | Out-Null
    $december = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=12" -WebSession $session)
    Assert (@($december | Where-Object { $_.description -eq 'Teste ajustado' }).Count -eq 0) 'A serie continuou apos o novo fim.'
    $november = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=11" -WebSession $session)
    Assert (@($november | Where-Object { $_.description -eq 'Teste ajustado' -and $_.amount -eq 120 -and $_.installmentCount -eq 2 }).Count -eq 1) 'Mes existente nao foi atualizado.'

    $edit.recurrenceEndDate = '2027-01-31'
    Send-Json 'Put' "/transactions/$($created.id)" $edit | Out-Null
    $december = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=12" -WebSession $session)
    Assert (@($december | Where-Object { $_.description -eq 'Teste ajustado' -and $_.installmentCount -eq 4 }).Count -eq 1) 'Dezembro nao voltou apos ampliar o prazo.'
    $january = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2027&month=1" -WebSession $session)
    Assert (@($january | Where-Object { $_.description -eq 'Teste ajustado' }).Count -eq 1) 'Janeiro nao foi gerado.'

    $middleOccurrence = @($december | Where-Object { $_.description -eq 'Teste ajustado' })[0]
    Invoke-RestMethod -Uri "$BaseUrl/transactions/$($middleOccurrence.id)" -Method Delete -WebSession $session -Headers (Get-CsrfHeaders) | Out-Null
    $october = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2026&month=10" -WebSession $session)
    $january = @(Invoke-RestMethod -Uri "$BaseUrl/transactions?year=2027&month=1" -WebSession $session)
    Assert (@($october | Where-Object { $_.description -eq 'Teste ajustado' }).Count -eq 0) 'A serie permaneceu em outubro.'
    Assert (@($january | Where-Object { $_.description -eq 'Teste ajustado' }).Count -eq 0) 'A serie foi recriada apos exclusao.'

    $single = Send-Json 'Post' '/transactions' @{
        type = 'Income'; description = 'Receita unica'; note = $null; amount = 50; date = '2026-10-09'
        status = 'Confirmed'; categoryId = $null; isRecurring = $false; isInstallment = $false
    }
    Send-Json 'Put' "/transactions/$($single.id)" @{
        description = 'Receita corrigida'; note = 'Teste'; amount = 75; date = '2026-10-10'
        categoryId = $null; recurrenceEndDate = $null; totalPurchaseAmount = $null
    } | Out-Null
    $detail = Invoke-RestMethod -Uri "$BaseUrl/transactions/$($single.id)" -WebSession $session
    Assert ($detail.amount -eq 75 -and $detail.date -eq '2026-10-10') 'Lancamento unico nao foi editado.'
    Invoke-RestMethod -Uri "$BaseUrl/transactions/$($single.id)" -Method Delete -WebSession $session -Headers (Get-CsrfHeaders) | Out-Null

    Write-Output 'PASS: detalhes, edicao, reducao e ampliacao de prazo, exclusao de serie e lancamento unico.'
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
            $parameter.Value = $email.ToUpperInvariant()
            $command.ExecuteNonQuery() | Out-Null
            $transaction.Commit()
        }
        catch { $transaction.Rollback(); throw }
    }
    finally { $connection.Dispose() }
}
