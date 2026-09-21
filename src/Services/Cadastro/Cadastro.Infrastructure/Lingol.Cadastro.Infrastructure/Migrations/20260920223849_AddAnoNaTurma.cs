using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Cadastro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnoNaTurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Professores_ProfessorId1",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_ProfessorId1",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "ProfessorId1",
                table: "Turmas");

            migrationBuilder.AddColumn<int>(
                name: "Ano",
                table: "Turmas",
                type: "int",
                nullable: false,
                defaultValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ano",
                table: "Turmas");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfessorId1",
                table: "Turmas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_ProfessorId1",
                table: "Turmas",
                column: "ProfessorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Professores_ProfessorId1",
                table: "Turmas",
                column: "ProfessorId1",
                principalTable: "Professores",
                principalColumn: "Id");
        }
    }
}
