using GestaoFinanceiraPessoal.Api.Contracts.Onboarding;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Domain.Enums;

namespace GestaoFinanceiraPessoal.Api.Services;

public static class OnboardingSetupService
{
    public static void AddInitialSetup(
        ApplicationDbContext dbContext,
        User user,
        CompleteOnboardingRequest request)
    {
        dbContext.FinancialProfiles.Add(new FinancialProfile
        {
            User = user,
            InitialCapital = request.InitialCapital,
            EmploymentType = request.EmploymentType,
            IncomeSetupMode = request.IncomeSetupMode,
            MonthlyIncomeReminderDay = request.IncomeSetupMode == IncomeSetupMode.MonthlyReminder
                ? request.IncomeReminderDay
                : null
        });

        if (request.IncomeSetupMode != IncomeSetupMode.FixedMonthly)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var dayOfMonth = request.IncomeDayOfMonth!.Value;
        var firstOccurrence = new DateOnly(
            nextMonth.Year,
            nextMonth.Month,
            Math.Min(dayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));

        dbContext.RecurringTransactions.Add(new RecurringTransaction
        {
            User = user,
            Type = TransactionType.Income,
            Description = "Renda mensal",
            Amount = request.FixedMonthlyIncome!.Value,
            DayOfMonth = dayOfMonth,
            StartDate = firstOccurrence,
            NextOccurrenceDate = firstOccurrence
        });
    }
}
