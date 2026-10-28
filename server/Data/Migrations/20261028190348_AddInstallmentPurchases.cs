using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "RecurringTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstallment",
                table: "RecurringTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPurchaseAmount",
                table: "RecurringTransactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "IsInstallment",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "TotalPurchaseAmount",
                table: "RecurringTransactions");
        }
    }
}
