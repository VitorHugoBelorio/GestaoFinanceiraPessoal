using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Transactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "RecurringTransactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "RecurringTransactions");
        }
    }
}
