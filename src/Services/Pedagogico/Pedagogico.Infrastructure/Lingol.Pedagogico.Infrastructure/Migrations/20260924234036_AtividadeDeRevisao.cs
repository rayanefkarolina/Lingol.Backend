using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Pedagogico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AtividadeDeRevisao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlunoId",
                table: "Atividades",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AtividadeOrigemId",
                table: "Atividades",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_AlunoId",
                table: "Atividades",
                column: "AlunoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Atividades_AlunoId",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "AlunoId",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "AtividadeOrigemId",
                table: "Atividades");
        }
    }
}
