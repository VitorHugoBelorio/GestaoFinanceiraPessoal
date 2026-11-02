using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarkFixedMonthlyIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMonthlyIncome",
                table: "RecurringTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE recurring
                SET IsMonthlyIncome = 1
                FROM RecurringTransactions AS recurring
                INNER JOIN FinancialProfiles AS profile ON profile.UserId = recurring.UserId
                WHERE profile.EmploymentType = 'Clt'
                  AND profile.IncomeSetupMode = 'FixedMonthly'
                  AND recurring.Type = 'Income'
                  AND recurring.Description = N'Renda mensal';

                UPDATE entry
                SET IsMonthlyIncome = 1
                FROM Transactions AS entry
                INNER JOIN RecurringTransactions AS recurring ON recurring.Id = entry.RecurringTransactionId
                WHERE recurring.IsMonthlyIncome = 1 AND entry.Type = 'Income';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMonthlyIncome",
                table: "RecurringTransactions");
        }
    }
}
