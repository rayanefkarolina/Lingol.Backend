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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Livro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapituloOuAssunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Materia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NumQuestoes = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    Modo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tema = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtividadeOrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    MensagemErro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayloadGeracaoJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DificuldadesAluno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RespostaAlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DificuldadesAluno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RespostasAluno",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    Acertos = table.Column<int>(type: "integer", nullable: false),
                    Erros = table.Column<int>(type: "integer", nullable: false),
                    TotalQuestoes = table.Column<int>(type: "integer", nullable: false),
                    CorrecaoProcessada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nota = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    FeedbackGeral = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasAluno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Enunciado = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TipoQuestao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Habilidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GabaritoOuCriterio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Explicacao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AlternativasJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RespostaAlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespostaEscolhida = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstaCorreta = table.Column<bool>(type: "boolean", nullable: false),
                    TempoSegundos = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_Atividades_AlunoId",
                table: "Atividades",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_Status",
                table: "Atividades",
                column: "Status");

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
