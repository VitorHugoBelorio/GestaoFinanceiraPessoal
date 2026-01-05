using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Migrations
{
    /// <inheritdoc />
    public partial class AddRecorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ano",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Mes",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Despesas");

            migrationBuilder.AddColumn<DateTime>(
                name: "Data",
                table: "Receitas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RecorrenciaId",
                table: "Receitas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecorrenciaId",
                table: "Despesas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Recorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativa = table.Column<bool>(type: "bit", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recorrencias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RecorrenciaId",
                table: "Receitas",
                column: "RecorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_RecorrenciaId",
                table: "Despesas",
                column: "RecorrenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Recorrencias_RecorrenciaId",
                table: "Despesas",
                column: "RecorrenciaId",
                principalTable: "Recorrencias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receitas_Recorrencias_RecorrenciaId",
                table: "Receitas",
                column: "RecorrenciaId",
                principalTable: "Recorrencias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Recorrencias_RecorrenciaId",
                table: "Despesas");

            migrationBuilder.DropForeignKey(
                name: "FK_Receitas_Recorrencias_RecorrenciaId",
                table: "Receitas");

            migrationBuilder.DropTable(
                name: "Recorrencias");

            migrationBuilder.DropIndex(
                name: "IX_Receitas_RecorrenciaId",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_RecorrenciaId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "RecorrenciaId",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "RecorrenciaId",
                table: "Despesas");

            migrationBuilder.AddColumn<int>(
                name: "Ano",
                table: "Receitas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mes",
                table: "Receitas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Receitas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Despesas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
