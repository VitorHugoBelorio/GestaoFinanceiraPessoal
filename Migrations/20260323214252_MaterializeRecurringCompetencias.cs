using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoFinanceiraPessoal.Migrations
{
    public partial class MaterializeRecurringCompetencias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receitas_RecorrenciaId",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_RecorrenciaId",
                table: "Despesas");

            migrationBuilder.AddColumn<int>(
                name: "DiaReferencia",
                table: "Recorrencias",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MesReferencia",
                table: "Recorrencias",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataReferenciaRecorrencia",
                table: "Receitas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EhModeloRecorrente",
                table: "Receitas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OcorrenciaCancelada",
                table: "Receitas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataReferenciaRecorrencia",
                table: "Despesas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EhModeloRecorrente",
                table: "Despesas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OcorrenciaCancelada",
                table: "Despesas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Despesas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pendente");

            migrationBuilder.Sql(@"
UPDATE r
SET
    DiaReferencia = COALESCE(receitaBase.Dia, despesaBase.Dia, DAY(r.ProximaExecucao), 1),
    MesReferencia = COALESCE(receitaBase.Mes, despesaBase.Mes, MONTH(r.ProximaExecucao), 1)
FROM Recorrencias r
OUTER APPLY (
    SELECT TOP 1 DAY(re.Data) AS Dia, MONTH(re.Data) AS Mes
    FROM Receitas re
    WHERE re.RecorrenciaId = r.Id
    ORDER BY re.Id
) receitaBase
OUTER APPLY (
    SELECT TOP 1 DAY(de.Data) AS Dia, MONTH(de.Data) AS Mes
    FROM Despesas de
    WHERE de.RecorrenciaId = r.Id
    ORDER BY de.Id
) despesaBase;

UPDATE Receitas
SET
    DataReferenciaRecorrencia = Data,
    EhModeloRecorrente = CASE WHEN RecorrenciaId IS NOT NULL THEN 1 ELSE 0 END,
    OcorrenciaCancelada = 0
WHERE RecorrenciaId IS NOT NULL;

INSERT INTO Receitas (Descricao, Valor, Data, DataReferenciaRecorrencia, EhModeloRecorrente, OcorrenciaCancelada, RecorrenciaId, UserId)
SELECT Descricao, Valor, Data, DataReferenciaRecorrencia, 0, 0, RecorrenciaId, UserId
FROM Receitas
WHERE RecorrenciaId IS NOT NULL AND EhModeloRecorrente = 1;

UPDATE Despesas
SET
    Status = CASE WHEN Status IS NULL OR LTRIM(RTRIM(Status)) = '' THEN 'Pendente' ELSE Status END,
    DataReferenciaRecorrencia = Data,
    EhModeloRecorrente = CASE WHEN RecorrenciaId IS NOT NULL THEN 1 ELSE 0 END,
    OcorrenciaCancelada = 0;

INSERT INTO Despesas (Descricao, Valor, Data, CategoriaId, DataReferenciaRecorrencia, EhModeloRecorrente, OcorrenciaCancelada, Status, RecorrenciaId, UserId)
SELECT Descricao, Valor, Data, CategoriaId, DataReferenciaRecorrencia, 0, 0, Status, RecorrenciaId, UserId
FROM Despesas
WHERE RecorrenciaId IS NOT NULL AND EhModeloRecorrente = 1;
");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RecorrenciaId_DataReferenciaRecorrencia",
                table: "Receitas",
                columns: new[] { "RecorrenciaId", "DataReferenciaRecorrencia" },
                unique: true,
                filter: "[RecorrenciaId] IS NOT NULL AND [DataReferenciaRecorrencia] IS NOT NULL AND [EhModeloRecorrente] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_RecorrenciaId_DataReferenciaRecorrencia",
                table: "Despesas",
                columns: new[] { "RecorrenciaId", "DataReferenciaRecorrencia" },
                unique: true,
                filter: "[RecorrenciaId] IS NOT NULL AND [DataReferenciaRecorrencia] IS NOT NULL AND [EhModeloRecorrente] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receitas_RecorrenciaId_DataReferenciaRecorrencia",
                table: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_RecorrenciaId_DataReferenciaRecorrencia",
                table: "Despesas");

            migrationBuilder.Sql(@"
DELETE FROM Receitas
WHERE RecorrenciaId IS NOT NULL AND EhModeloRecorrente = 0;

DELETE FROM Despesas
WHERE RecorrenciaId IS NOT NULL AND EhModeloRecorrente = 0;
");

            migrationBuilder.DropColumn(
                name: "DiaReferencia",
                table: "Recorrencias");

            migrationBuilder.DropColumn(
                name: "MesReferencia",
                table: "Recorrencias");

            migrationBuilder.DropColumn(
                name: "DataReferenciaRecorrencia",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "EhModeloRecorrente",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "OcorrenciaCancelada",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "DataReferenciaRecorrencia",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "EhModeloRecorrente",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "OcorrenciaCancelada",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Despesas");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RecorrenciaId",
                table: "Receitas",
                column: "RecorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_RecorrenciaId",
                table: "Despesas",
                column: "RecorrenciaId");
        }
    }
}
