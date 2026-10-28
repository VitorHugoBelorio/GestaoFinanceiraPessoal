using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReminderDayOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE FinancialProfiles SET MonthlyIncomeReminderDay = 28 WHERE MonthlyIncomeReminderDay > 28");

            migrationBuilder.DropColumn(
                name: "MonthlyIncomeReminderStartDate",
                table: "FinancialProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "MonthlyIncomeReminderStartDate",
                table: "FinancialProfiles",
                type: "date",
                nullable: true);
        }
    }
}
