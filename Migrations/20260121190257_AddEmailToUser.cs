using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativa",
                table: "Recorrencias");

            migrationBuilder.RenameColumn(
                name: "DataInicio",
                table: "Recorrencias",
                newName: "ProximaExecucao");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Recorrencias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Intervalo",
                table: "Recorrencias",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Intervalo",
                table: "Recorrencias");

            migrationBuilder.RenameColumn(
                name: "ProximaExecucao",
                table: "Recorrencias",
                newName: "DataInicio");

            migrationBuilder.AlterColumn<int>(
                name: "Tipo",
                table: "Recorrencias",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "Ativa",
                table: "Recorrencias",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
