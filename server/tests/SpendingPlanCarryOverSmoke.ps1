param([string]$BaseUrl = 'http://localhost:5134/api')

$ErrorActionPreference = 'Stop'
$email = "spending-plan-smoke-$([guid]::NewGuid().ToString('N'))@example.invalid"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-CsrfHeaders {
    $csrf = Invoke-RestMethod -Uri "$BaseUrl/auth/csrf" -WebSession $session
    return @{ 'X-CSRF-TOKEN' = $csrf.token }
}

function Send-Json($Method, $Path, $Body) {
    Invoke-RestMethod -Uri "$BaseUrl$Path" -Method $Method -WebSession $session -ContentType 'application/json' -Headers (Get-CsrfHeaders) -Body ($Body | ConvertTo-Json -Depth 8)
}

function Get-Month($Year, $Month) {
    return Invoke-RestMethod -Uri "$BaseUrl/dashboard?year=$Year&month=$Month" -WebSession $session
}

function Assert($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}

try {
    Send-Json 'Post' '/auth/register-and-onboard' @{
        account = @{ name = 'Teste temporario'; email = $email; password = 'Teste-Local-2026!' }
        setup = @{ initialCapital = 5000; employmentType = 'Clt'; incomeSetupMode = 'Manual'; fixedMonthlyIncome = $null; incomeDayOfMonth = $null; incomeReminderDay = $null }
    } | Out-Null

    $futureMonth = Get-Month 2026 12
    Assert ($null -eq $futureMonth.monthlySpendingBaseAmount -and $futureMonth.monthlySpendingCarryIn -eq 0 -and $futureMonth.monthlySpendingCarryOverUnused -eq $false) 'O planejamento de um mes futuro sem limite deve retornar valores completos.'
    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2026; month = 12; amount = 1500; carryOverUnused = $false } | Out-Null
    $futureMonth = Get-Month 2026 12
    Assert ($futureMonth.monthlySpendingBaseAmount -eq 1500 -and $futureMonth.monthlySpendingPlan -eq 1500) 'Nao foi possivel salvar o limite de um mes futuro.'

    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 1; amount = 1500; carryOverUnused = $true } | Out-Null
    $expense = Send-Json 'Post' '/transactions' @{
        type = 'Expense'; description = 'Gastos do mes'; amount = 1300; date = '2025-01-15'
        status = 'Confirmed'; categoryId = $null; isRecurring = $false; isInstallment = $false
    }

    $before = Get-Month 2025 1
    Assert ($before.monthlySpendingRemaining -eq 200) 'A sobra inicial deveria ser R$ 200,00.'
    $pending = Get-Month 2025 2
    Assert ($null -eq $pending.monthlySpendingPlan -and $pending.monthlySpendingCarryIn -eq 200) 'A sobra deveria aguardar o novo limite, sem criar uma receita ou um limite automatico.'
    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 2; amount = 1500; carryOverUnused = $true } | Out-Null
    $february = Get-Month 2025 2
    Assert ($february.monthlySpendingBaseAmount -eq 1500 -and $february.monthlySpendingCarryIn -eq 200 -and $february.monthlySpendingPlan -eq 1700) 'A sobra nao foi somada ao limite de fevereiro.'
    Assert ($february.totalBalance -eq 3700 -and $february.availableBalance -eq 3700) 'O planejamento alterou o saldo real.'

    Send-Json 'Put' "/transactions/$($expense.id)" @{
        description = 'Gastos corrigidos'; note = $null; amount = 1400; date = '2025-01-15'
        categoryId = $null; recurrenceEndDate = $null; totalPurchaseAmount = $null
    } | Out-Null
    $february = Get-Month 2025 2
    Assert ($february.monthlySpendingCarryIn -eq 100 -and $february.monthlySpendingPlan -eq 1600) 'A sobra nao acompanhou a correcao da despesa.'

    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 1; amount = 1500; carryOverUnused = $false } | Out-Null
    $february = Get-Month 2025 2
    Assert ($february.monthlySpendingCarryIn -eq 0 -and $february.monthlySpendingPlan -eq 1500) 'A sobra continuou acumulando apos desmarcar a opcao.'

    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 1; amount = 1500; carryOverUnused = $true } | Out-Null
    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 3; amount = 1000; carryOverUnused = $false } | Out-Null
    $march = Get-Month 2025 3
    Assert ($march.monthlySpendingCarryIn -eq 1600 -and $march.monthlySpendingPlan -eq 2600) 'A sobra nao continuou pela cadeia de meses marcados.'

    Send-Json 'Put' '/investments/monthly-spending-plan' @{ year = 2025; month = 2; amount = 1500; carryOverUnused = $false } | Out-Null
    $march = Get-Month 2025 3
    Assert ($march.monthlySpendingCarryIn -eq 0 -and $march.monthlySpendingPlan -eq 1000) 'A cadeia nao parou apos desmarcar fevereiro.'

    Write-Output 'PASS: sobra acumulada, saldo real preservado, recalculo apos edicao e interrupcao da cadeia.'
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
