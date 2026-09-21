using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Pedagogico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHabilidadeNaQuestao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Habilidade",
                table: "Questoes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Habilidade",
                table: "Questoes");
        }
    }
}
