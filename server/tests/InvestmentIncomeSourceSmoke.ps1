param([string]$BaseUrl = 'http://localhost:5133/api')

$ErrorActionPreference = 'Stop'
$emails = @()
$today = [DateTime]::Today
$date = $today.ToString('yyyy-MM-dd')

function Assert($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}

function Send-Json($Session, $Method, $Path, $Body) {
    $csrf = Invoke-RestMethod -Uri "$BaseUrl/auth/csrf" -WebSession $Session
    Invoke-RestMethod -Uri "$BaseUrl$Path" -Method $Method -WebSession $Session `
        -ContentType 'application/json' -Headers @{ 'X-CSRF-TOKEN' = $csrf.token } `
        -Body ($Body | ConvertTo-Json -Depth 8)
}

function Assert-Rejected($Session, $Method, $Path, $Body, $Message, [int]$ExpectedStatus = 400) {
    try {
        Send-Json $Session $Method $Path $Body | Out-Null
        throw $Message
    }
    catch {
        if ($_.Exception.Message -eq $Message) { throw }
        Assert ([int]$_.Exception.Response.StatusCode -eq $ExpectedStatus) $Message
    }
}

try {
    $firstUserAllocationId = $null
    foreach ($employmentType in @('Clt', 'Pj')) {
        $email = "income-source-$([guid]::NewGuid().ToString('N'))@example.invalid"
        $emails += $email
        $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        Send-Json $session 'Post' '/auth/register-and-onboard' @{
            account = @{ name = 'Teste temporario'; email = $email; password = 'Teste-Local-2026!' }
            setup = @{ initialCapital = 1000; employmentType = $employmentType; incomeSetupMode = 'Manual'; fixedMonthlyIncome = $null; incomeDayOfMonth = $null; incomeReminderDay = $null }
        } | Out-Null

        $incomeContribution = @{ type = 'Contribution'; amount = 500; date = $date; note = $null; isFromMonthlyIncome = $true }
        Assert-Rejected $session 'Post' '/investments/movements' $incomeContribution 'Um aporte da renda sem receita registrada deveria ser recusado.'

        Send-Json $session 'Post' '/transactions' @{
            type = 'Income'; description = 'Renda mensal'; amount = 2000; date = $date
            status = 'Confirmed'; categoryId = $null; isRecurring = $false; isInstallment = $false
            isMonthlyIncome = $true
        } | Out-Null
        $extraIncome = Send-Json $session 'Post' '/transactions' @{
            type = 'Income'; description = 'Venda eventual'; amount = 100; date = $date
            status = 'Confirmed'; categoryId = $null; isRecurring = $false; isInstallment = $false
            isMonthlyIncome = $false
        }

        $movement = Send-Json $session 'Post' '/investments/movements' $incomeContribution
        Assert ($movement.isFromMonthlyIncome -eq $true) 'A origem do aporte nao foi guardada.'
        Assert-Rejected $session 'Post' '/investments/movements' @{ type = 'Contribution'; amount = 1700; date = $date; isFromMonthlyIncome = $true } 'Aporte maior que a renda ainda livre deveria ser recusado.'
        Send-Json $session 'Post' '/investments/movements' @{
            type = 'Contribution'; amount = 200; date = $date; isFromMonthlyIncome = $false
        } | Out-Null

        $dashboard = Invoke-RestMethod -Uri "$BaseUrl/dashboard?year=$($today.Year)&month=$($today.Month)" -WebSession $session
        Assert ($dashboard.totalBalance -eq 3100 -and $dashboard.availableBalance -eq 2400 -and $dashboard.investedBalance -eq 700) 'O aporte alterou o saldo total ou as posicoes incorretamente.'
        Assert ($dashboard.monthlyIncomeInvested -eq 500 -and $dashboard.monthlyContributions -eq 700) 'O total investido da renda foi calculado incorretamente.'
        Assert ($dashboard.monthlyPrimaryIncome -eq 2000 -and $dashboard.monthlyIncome -eq 2100) 'O recebimento principal nao foi separado das outras receitas.'
        Send-Json $session 'Put' "/transactions/$($extraIncome.id)" @{
            description = 'Venda eventual'; note = $null; amount = 100; date = $date
            categoryId = $null; recurrenceEndDate = $null; totalPurchaseAmount = $null; isMonthlyIncome = $true
        } | Out-Null
        $dashboard = Invoke-RestMethod -Uri "$BaseUrl/dashboard?year=$($today.Year)&month=$($today.Month)" -WebSession $session
        Assert ($dashboard.monthlyPrimaryIncome -eq 2100) 'A edicao nao atualizou o recebimento principal.'
        Send-Json $session 'Put' "/transactions/$($extraIncome.id)" @{
            description = 'Venda eventual'; note = $null; amount = 100; date = $date
            categoryId = $null; recurrenceEndDate = $null; totalPurchaseAmount = $null; isMonthlyIncome = $false
        } | Out-Null

        $allocations = Invoke-RestMethod -Uri "$BaseUrl/investments/allocations" -WebSession $session
        Assert ($allocations.Count -eq 0) 'Os grupos de outra conta ficaram visiveis.'
        if ($employmentType -eq 'Pj') {
            Assert-Rejected $session 'Put' "/investments/allocations/$firstUserAllocationId" @{ name = 'Grupo indevido'; amount = 1 } 'Uma conta nao pode alterar grupos de outra conta.' 404
        }
        $cdb = Send-Json $session 'Post' '/investments/allocations' @{ name = 'CDB'; amount = 400 }
        if ($employmentType -eq 'Clt') { $firstUserAllocationId = $cdb.id }
        $fii = Send-Json $session 'Post' '/investments/allocations' @{ name = 'Fundos imobiliarios'; amount = 200 }
        Assert-Rejected $session 'Post' '/investments/allocations' @{ name = 'Imovel'; amount = 200 } 'Os grupos nao podem superar o total aplicado.'
        Send-Json $session 'Put' "/investments/allocations/$($cdb.id)" @{ name = 'Renda fixa'; amount = 450 } | Out-Null
        Assert-Rejected $session 'Post' '/investments/movements' @{ type = 'Withdrawal'; amount = 300; date = $date } 'O resgate nao pode deixar grupos maiores que o total aplicado.'
        $allocations = Invoke-RestMethod -Uri "$BaseUrl/investments/allocations" -WebSession $session
        Assert ($allocations.Count -eq 2 -and ($allocations | Measure-Object -Property amount -Sum).Sum -eq 650) 'Os grupos da carteira nao foram guardados corretamente.'
        $csrf = Invoke-RestMethod -Uri "$BaseUrl/auth/csrf" -WebSession $session
        Invoke-RestMethod -Uri "$BaseUrl/investments/allocations/$($fii.id)" -Method Delete -WebSession $session -Headers @{ 'X-CSRF-TOKEN' = $csrf.token } | Out-Null
        $dashboard = Invoke-RestMethod -Uri "$BaseUrl/dashboard?year=$($today.Year)&month=$($today.Month)" -WebSession $session
        Assert ($dashboard.investedBalance -eq 700 -and $dashboard.totalBalance -eq 3100) 'Excluir um grupo alterou o investimento ou o saldo total.'
    }

    $fixedEmail = "fixed-income-$([guid]::NewGuid().ToString('N'))@example.invalid"
    $emails += $fixedEmail
    $fixedSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Send-Json $fixedSession 'Post' '/auth/register-and-onboard' @{
        account = @{ name = 'Teste renda fixa'; email = $fixedEmail; password = 'Teste-Local-2026!' }
        setup = @{ initialCapital = 1000; employmentType = 'Clt'; incomeSetupMode = 'FixedMonthly'; fixedMonthlyIncome = 1500; incomeDayOfMonth = 10; incomeReminderDay = $null }
    } | Out-Null
    $nextMonth = $today.AddMonths(1)
    $futureEntries = Invoke-RestMethod -Uri "$BaseUrl/transactions?year=$($nextMonth.Year)&month=$($nextMonth.Month)" -WebSession $fixedSession
    $fixedEntry = $futureEntries | Where-Object { $_.description -eq 'Renda mensal' } | Select-Object -First 1
    Assert ($null -ne $fixedEntry -and $fixedEntry.isMonthlyIncome -eq $true -and $fixedEntry.status -eq 'Planned') 'A renda fixa CLT deve nascer identificada, mas sem contar como recebida antes da data.'

    Write-Output 'PASS: recebimento principal CLT/PJ, renda fixa, aporte da renda, grupos e limites.'
}
finally {
    $config = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\appsettings.Development.json') -Raw | ConvertFrom-Json
    $connection = New-Object System.Data.SqlClient.SqlConnection $config.ConnectionStrings.DefaultConnection
    try {
        $connection.Open()
        foreach ($email in $emails) {
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
    DELETE FROM InvestmentAllocations WHERE UserId = @UserId;
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
    }
    finally { $connection.Dispose() }
}
