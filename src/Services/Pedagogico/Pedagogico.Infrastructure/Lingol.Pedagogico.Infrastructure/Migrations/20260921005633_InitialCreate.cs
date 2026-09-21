using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lingol.Pedagogico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Atividades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Livro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CapituloOuAssunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Materia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumQuestoes = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    Modo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MensagemErro = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DificuldadesAluno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespostaAlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DificuldadesAluno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RespostasAluno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Acertos = table.Column<int>(type: "int", nullable: false),
                    Erros = table.Column<int>(type: "int", nullable: false),
                    TotalQuestoes = table.Column<int>(type: "int", nullable: false),
                    CorrecaoProcessada = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Nota = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FeedbackGeral = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasAluno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Enunciado = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TipoQuestao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GabaritoOuCriterio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Explicacao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AlternativasJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questoes_Atividades_AtividadeId",
                        column: x => x.AtividadeId,
                        principalTable: "Atividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostasQuestao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespostaAlunoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespostaEscolhida = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EstaCorreta = table.Column<bool>(type: "bit", nullable: false),
                    TempoSegundos = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasQuestao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostasQuestao_RespostasAluno_RespostaAlunoId",
                        column: x => x.RespostaAlunoId,
                        principalTable: "RespostasAluno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_TurmaId",
                table: "Atividades",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_DificuldadesAluno_TurmaId_AlunoId",
                table: "DificuldadesAluno",
                columns: new[] { "TurmaId", "AlunoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questoes_AtividadeId_Ordem",
                table: "Questoes",
                columns: new[] { "AtividadeId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAluno_AtividadeId_AlunoId",
                table: "RespostasAluno",
                columns: new[] { "AtividadeId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAluno_TurmaId",
                table: "RespostasAluno",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasQuestao_QuestaoId",
                table: "RespostasQuestao",
                column: "QuestaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasQuestao_RespostaAlunoId",
                table: "RespostasQuestao",
                column: "RespostaAlunoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DificuldadesAluno");

            migrationBuilder.DropTable(
                name: "Questoes");

            migrationBuilder.DropTable(
                name: "RespostasQuestao");

            migrationBuilder.DropTable(
                name: "Atividades");

            migrationBuilder.DropTable(
                name: "RespostasAluno");
        }
    }
}
