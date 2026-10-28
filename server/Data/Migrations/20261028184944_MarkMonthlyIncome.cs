using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarkMonthlyIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMonthlyIncome",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMonthlyIncome",
                table: "Transactions");
        }
    }
}
