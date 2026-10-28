using GestaoFinanceiraPessoal.Api.Contracts.Transactions;
using GestaoFinanceiraPessoal.Api.Contracts.Investments;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Contracts.Dashboard;

public record DashboardResponse(
    int Year,
    int Month,
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    decimal MonthlyBalance,
    bool IncomeReminderDue,
    bool MonthlyIncomeRecorded,
    EmploymentType EmploymentType,
    IReadOnlyCollection<TransactionResponse> RecentTransactions,
    decimal InitialCapital,
    decimal OpeningInvestedAmount,
    decimal InvestedBalance,
    decimal AvailableBalance,
    decimal? MonthlySpendingPlan,
    decimal? MonthlySpendingRemaining,
    decimal MonthlyContributions,
    decimal MonthlyWithdrawals,
    IReadOnlyCollection<InvestmentMovementResponse> MonthlyInvestmentMovements);
