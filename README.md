# Lingol.Backend

Resumo
-------
- Projeto backend da plataforma Lingol: ambiente para professores (admins) e alunos.
- Objetivo: professor cria turmas, cadastra alunos e publica atividades geradas por IA (a partir de livro/capítulo/assunto). Alunos respondem; sistema corrige com IA, registra dificuldades e gera relatórios para o professor.
- Domínio inicial: Língua Portuguesa.

Status atual (implementado)
----------------------------
- Arquitetura baseada em Clean Architecture / microserviços (.NET 9).
- Projetos principais:
  - Lingol.Cadastro.* (Domain, Infrastructure, API) — entidades: Professor, Aluno, Turma, PerfilAee. CadastroDbContext disponível.
  - Lingol.Pedagogico.* (Domain, Infrastructure, Application, API) — entidades: Atividade, Questao, RespostaAluno, DificuldadeAluno, TipoDificuldade.
- Integrações:
  - IaHttpClient (Pedagogico.Infrastructure) para consumo do serviço de IA.
  - CadastroHttpClient (Pedagogico.Infrastructure) para consultar dados do serviço de Cadastro.
- Autenticação/Autorização:
  - /api/auth/professor/register (registro e JWT).
  - /api/auth/aluno/login (login por nome + matrícula, retorna JWT com claims mínimos).
- Queries já existentes: ObterRelatorioDificuldadesAlunoQuery, ObterRelatorioDificuldadesTurmaQuery.

O que falta (prioridade)
-----------------------
1. Módulo de Cadastro
   - POST /api/turmas (criar turma pelo professor)
   - POST /api/turmas/{turmaId}/alunos (adicionar aluno na turma)
   - GET /api/turmas (listar turmas do professor logado)
   - GET /api/turmas/{turmaId}/alunos (listar alunos da turma)

2. Fluxo do aluno
   - Garantir claims de turma no JWT do aluno
   - GET /api/turmas/{turmaId}/atividades
   - GET /api/atividades/{atividadeId}

3. Geração de atividade com IA
   - POST /api/atividades/gerar
   - Command/Handler GerarAtividadeCommand (Pedagogico.Application)
   - Persistir Atividade e Questao vinculadas à turma

4. Envio de respostas e correção
   - POST /api/atividades/{atividadeId}/respostas
   - Chamada à IA para correção/diagnóstico de dificuldades
   - Persistir DificuldadeAluno / TipoDificuldade

5. Relatórios
   - GET /api/relatorios/aluno/{alunoId}
   - GET /api/relatorios/turma/{turmaId}

Recomendações técnicas
----------------------
- Autorização: usar [Authorize(Roles = "Professor")] e validação de claims para alunos.
- IA: padronizar DTOs de request/response; incluir metadados no prompt (livro, capítulo, nível).
- Processamento: considerar correção em background (BackgroundService / fila) para escala.
- Persistência: garantir migrações EF Core para ambos os DbContexts.

Como validar localmente (resumo)
--------------------------------
1. Registrar professor e realizar login; copiar token JWT.
2. Criar turma (Bearer token do professor).
3. Cadastrar alunos na turma.
4. Login de aluno (nome + matrícula) e obter token.
5. Gerar atividade (POST /api/atividades/gerar) com payload básico (turmaId, livro, cap/assunto).
6. Aluno obtém atividade e envia respostas.
7. Verificar correção/relatórios.

Próximos passos sugeridos (tickets)
----------------------------------
- T1: Implementar CRUD de Turma e endpoints de alunos.
- T2: Implementar geração de atividade (IA) e persistência.
- T3: Implementar envio de respostas + correção IA (inicialmente síncrona, depois assíncrona).
- T4: Implementar endpoints de relatório e DTOs de apresentação.
- T5: Adicionar testes unitários para handlers críticos.

Contato
-------
Repositório: https://github.com/rayanefkarolina/Lingol.Backend

Como executar e desenvolver localmente
------------------------------------
Requisitos:
- .NET 9 SDK
- SQL Server (local ou container)
- RabbitMQ (opcional, se usar Worker/Message Bus)

Passos básicos (CLI):

1. Restaurar e compilar solução:

   dotnet restore
   dotnet build Lingol.Backend.sln

2. Aplicar migrações (EF Core) para cada contexto

   // Cadastro
   cd src/Services/Cadastro/Cadastro.Infrastructure/Lingol.Cadastro.Infrastructure
   dotnet ef migrations add InicialCadastro -p ../../Lingol.Cadastro.API -s ../../Lingol.Cadastro.API --context CadastroDbContext
   dotnet ef database update -p ../../Lingol.Cadastro.API -s ../../Lingol.Cadastro.API --context CadastroDbContext

   // Pedagógico
   cd src/Services/Pedagogico/Pedagogico.Infrastructure/Lingol.Pedagogico.Infrastructure
   dotnet ef migrations add InicialPedagogico -p ../../Lingol.Pedagogico.API -s ../../Lingol.Pedagogico.API --context PedagogicoDbContext
   dotnet ef database update -p ../../Lingol.Pedagogico.API -s ../../Lingol.Pedagogico.API --context PedagogicoDbContext

   Observação: Ajuste as strings de conexão em appsettings.Development.json conforme seu ambiente.

3. Executar APIs (do diretório da solução ou usando Visual Studio):

   // Executar Cadastro.API
   dotnet run --project src/Services/Cadastro/Cadastro.API/Lingol.Cadastro.API

   // Executar Pedagogico.API
   dotnet run --project src/Services/Pedagogico/Pedagogico.API/Lingol.Pedagogico.API

4. Testes

   dotnet test tests/Cadastro.Tests/Lingol.Cadastro.Tests
   dotnet test tests/Pedagogico.Tests/Lingol.Pedagogico.Tests

Observações finais
------------------
- Se preferir, use o Visual Studio para executar ambos os projetos com múltiplos perfis de inicialização.
- As chamadas ao serviço de IA dependem de variável de configuração IaProvider:BaseUrl; por padrão está apontando para http://localhost:5010/.
- Para produção, migre a correção síncrona para um worker assíncrono (BackgroundService + fila/RabbitMQ) e adicione retry/observability nas chamadas à IA.
