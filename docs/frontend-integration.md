# Integração Frontend ↔ Lingol Backend

Este documento descreve os endpoints públicos que o frontend deve consumir, formatos de requisição/resposta e detalhes de autenticação. Baseado no ambiente de desenvolvimento encontrado no repositório.

Obs: em desenvolvimento os services costumam rodar em:
- Cadastro API: http://localhost:5030 (ver Properties/launchSettings.json)
- Pedagógico API: http://localhost:5209 (ver Properties/launchSettings.json)

Autenticação
- Esquema: JWT Bearer (Authorization: "Bearer {token}")
- Tokens são emitidos por /api/auth/* (Cadastro.API)
- Claims relevantes no token:
  - ClaimTypes.NameIdentifier => id do usuário (Aluno/Professor)
  - ClaimTypes.Role / "role" => "Aluno" ou "Professor" (controle de acesso)
  - "turmaId" => id da turma (presente no token do aluno)
  - "matricula" e "perfilAee" (quando aplicável)

Endpoints - Serviço: Cadastro (Base URL dev: http://localhost:5030)

1) Autenticação / Registro
- POST /api/auth/professor/login
  - Permissão: AllowAnonymous
  - Body: { "Email": string, "Senha": string }
  - Response: { AccessToken, ExpiraEm, ProfessorId, NomeProfessor, Email }

- POST /api/auth/aluno/login
  - Permissão: AllowAnonymous
  - Body: { "Nome": string, "Matricula": string }
  - Response: { AccessToken, ExpiraEm, AlunoId, TurmaId, NomeAluno, Matricula }

- POST /api/auth/professor/register
  - Permissão: AllowAnonymous
  - Body: { "Nome": string, "Email": string, "Senha": string }
  - Response: { Id, Nome, Email }

2) Alunos
- GET /api/alunos/{alunoId}
  - Permissão: Authorize(Roles = "Professor,Aluno")
  - Nota: Aluno só pode obter seu próprio recurso; professor pode obter qualquer aluno
  - Response: { Id, Nome, Matricula, TurmaId, TipoNecessidade? }

- GET /api/alunos/turma/{turmaId}
  - Permissão: Professor
  - Response: array de alunos (mesmo DTO acima)

3) Turmas
- POST /api/turmas
  - Permissão: Professor
  - Body: CriarTurmaRequest (ver contrato) => { Nome, Materia }
  - Response: TurmaCadastroDto { Id, Nome, Materia, ProfessorId }

- GET /api/turmas
  - Permissão: Professor (retorna turmas do professor logado)
  - Response: array de TurmaCadastroDto

- GET /api/turmas/{turmaId}
  - Permissão: Professor
  - Response: TurmaCadastroDto

- POST /api/turmas/{turmaId}/alunos
  - Permissão: Professor
  - Body: CadastrarAlunoRequest { Nome, Matricula }
  - Response: AlunoCadastroDto

- GET /api/turmas/{turmaId}/alunos
  - Permissão: Professor
  - Response: array de AlunoCadastroDto

- GET /api/turmas/minhas
  - Permissão: Aluno
  - Response: array com a(s) turma(s) do aluno (TurmaCadastroDto)

Observações (Cadastro)
- Health: GET /health (AllowAnonymous) retorna { status, service }
- Swagger disponível em /swagger quando em Development

Endpoints - Serviço: Pedagógico (Base URL dev: http://localhost:5209)

1) Atividades
- GET /api/atividades/turmas/{turmaId}
  - Permissão: Authorize (Professor e Aluno)
  - Response: lista com itens: { Id, Livro, CapituloOuAssunto, QuestoesCount }

- GET /api/atividades/{atividadeId}
  - Permissão: Authorize
  - Response: { Id, Livro, CapituloOuAssunto, Questoes: [{ Id, Enunciado, TipoQuestao, AlternativasJson }] }

- POST /api/atividades/gerar
  - Permissão: Professor (Authorize(Roles = "Professor"))
  - Body: { "TurmaId": guid, "Livro": string, "CapituloOuAssunto": string, "Materia": string, "PerfilAeeContexto": string? }
  - Response: 201 Created com payload { AtividadeId, Questoes: [QuestaoGeradaDto] }
	- QuestaoGeradaDto: { Enunciado, Tipo, Alternativas: string[], GabaritoOuCriterio }

- POST /api/atividades/{atividadeId}/respostas
  - Permissão: Aluno (Authorize(Roles = "Aluno"))
  - Body: { "RespostasJson": string? } (formato livre; atualmente salvo como JSON string)
  - Response: { respostaId, nota, feedback }

Observações (Pedagógico)
- Health: GET /health (AllowAnonymous) retorna { status, service }
- Swagger disponível em /swagger
- Há integração com um provedor de IA via HttpClient (configurável em IaProvider:BaseUrl)

Cabeçalhos e erros comuns
- Sempre enviar Authorization: Bearer {token} para endpoints protegidos
- 401 => token faltando/expirado
- 403 => token válido mas sem permissão (role ou acesso à turma)
- 404 => recurso não encontrado

Sugestões para frontend
- Implementar um service de Auth que armazene o token e as claims essenciais (user id, role, turmaId)
- Em chamadas que exigem role específica, verificar localmente antes de chamar o back para UX melhor (mas sempre confiar no backend para segurança)
- Usar os endpoints /health e /swagger para debug e verificação em ambiente de desenvolvimento

Onde encontrar mais detalhes no código
- src/Services/Cadastro/Cadastro.API/... (controllers de autenticação, alunos e turmas)
- src/Services/Pedagogico/Pedagogico.API/... (AtividadesController)
- src/BuildingBlocks/Lingol.Contracts/... (tipos de request/response usados pelos controllers)

---
Arquivo gerado automaticamente a partir do repositório. Ajustar base URLs conforme ambiente (dev/staging/prod).
