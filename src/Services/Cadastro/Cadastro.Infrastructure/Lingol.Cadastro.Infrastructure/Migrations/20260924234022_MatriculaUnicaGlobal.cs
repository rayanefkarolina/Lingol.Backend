using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Cadastro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MatriculaUnicaGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula_TurmaId",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos",
                column: "Matricula",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alunos_Matricula",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Matricula_TurmaId",
                table: "Alunos",
                columns: new[] { "Matricula", "TurmaId" },
                unique: true);
        }
    }
}
