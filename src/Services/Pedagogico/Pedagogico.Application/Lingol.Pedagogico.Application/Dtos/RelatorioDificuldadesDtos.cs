using Lingol.Pedagogico.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Dtos
{
    public record DificuldadeTurmaResumoDto(
    TipoDificuldade Tipo,
    int QuantidadeTotal,
    int QuantidadeAlunosAfetados,
    List<Guid> QuestoesComDificuldade);

        public record DificuldadeResumoDto(
            TipoDificuldade Tipo,
            int Quantidade,
            List<Guid> QuestoesComDificuldade);

        public record DificuldadePorAlunoDto(
            Guid AlunoId,
            string NomeAluno,
            decimal? NotaMedia,
            List<DificuldadeResumoDto> Dificuldades);

        public record RelatorioDificuldadesTurmaResult(
            Guid TurmaId,
            string NomeTurma,
            List<DificuldadeTurmaResumoDto> ResumoTurma,
            List<DificuldadePorAlunoDto> Alunos);

        public record RelatorioDificuldadesAlunoResult(
            Guid TurmaId,
            Guid AlunoId,
            string NomeAluno,
            decimal? NotaMedia,
            List<DificuldadeResumoDto> Dificuldades);
 }

