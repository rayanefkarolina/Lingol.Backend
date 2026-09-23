using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Pedagogico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemaNaAtividade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "Atividades",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tema",
                table: "Atividades");
        }
    }
}
