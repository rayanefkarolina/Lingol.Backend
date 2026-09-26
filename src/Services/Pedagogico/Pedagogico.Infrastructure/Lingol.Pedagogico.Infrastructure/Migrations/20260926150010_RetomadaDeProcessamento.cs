using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Pedagogico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetomadaDeProcessamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadGeracaoJson",
                table: "Atividades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_Status",
                table: "Atividades",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Atividades_Status",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "PayloadGeracaoJson",
                table: "Atividades");
        }
    }
}
