namespace GestaoFinanceiraPessoal.Api.Domain;

public static class RecurrenceSchedule
{
    public static DateOnly GetNextMonthlyDate(DateOnly currentDate, int preferredDay)
    {
        var nextMonth = new DateOnly(currentDate.Year, currentDate.Month, 1).AddMonths(1);
        var day = Math.Min(preferredDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        return new DateOnly(nextMonth.Year, nextMonth.Month, day);
    }

    public static int CountOccurrences(DateOnly startDate, DateOnly endDate, int preferredDay)
    {
        if (endDate < startDate) return 0;

        var count = 0;
        var occurrence = startDate;
        while (occurrence <= endDate)
        {
            count++;
            if (occurrence.Year == 9999 && occurrence.Month == 12) break;
            occurrence = GetNextMonthlyDate(occurrence, preferredDay);
        }

        return count;
    }
}
