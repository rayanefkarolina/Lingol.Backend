# Rodando o Lingol.Backend localmente

Estado após a Fase 0: a solução compila com 0 erros, os dois bancos estão criados e o ciclo
pedagógico completo foi validado de ponta a ponta (professor → turma → aluno → atividade via fila
→ resposta → "acertou 6/10").

## JWT

A chave `Jwt:Key` precisa ter **no mínimo 32 bytes** — HS256 exige 256 bits. A chave anterior
(`ChaveSuperSecretaLingol123!`, 27 bytes) fazia todo login retornar 500. A chave de
desenvolvimento atual está em `appsettings.json`; em produção use `dotnet user-secrets`
ou variável de ambiente, e mantenha o mesmo valor nos dois serviços.

## Pré-requisitos

| Item | Situação nesta máquina |
|---|---|
| SDK .NET | 10.0.301 instalado — compila os projetos `net9.0` normalmente |
| Runtime ASP.NET Core 9 | **não instalado** (há 5, 6, 8 e 10) |
| SQL Server | instâncias `SQLEXPRESS` e `ABLOCALHOST` (serviço parado por padrão) |
| dotnet-ef | atualizado para 9.0.20 (a 8.0.4 não funciona com EF Core 9) |

### Runtime .NET 9

Enquanto o runtime 9 não estiver instalado, use roll-forward para o runtime 10:

```
set DOTNET_ROLL_FORWARD=LatestMajor
```

Opções definitivas (escolher uma):
1. Instalar o **ASP.NET Core Runtime 9.0** (mantém `net9.0` como o produto pede);
2. Fixar `<RollForward>LatestMajor</RollForward>` nos `.csproj` das APIs/Worker;
3. Retargetar a solução para `net10.0`.

### SQL Server

O serviço precisa estar em execução (exige prompt de administrador):

```
net start MSSQL$SQLEXPRESS
```

Esta máquina tem 8 GB de RAM e o SQL Express fica sem fôlego quando as duas APIs, o
`ng serve` e o Visual Studio rodam juntos. Sintomas já observados:

- `Connection Timeout Expired ... post-login phase` — resolvido com `Connect Timeout=60`
  nas connection strings e `EnableRetryOnFailure` nos dois DbContext;
- banco em **RECOVERY_PENDING** e, na sequência, a instância parando de responder até ao
  handshake de pré-login. Nesse estado só um restart do serviço resolve:

```
net stop MSSQL$SQLEXPRESS && net start MSSQL$SQLEXPRESS
```

Se o banco continuar em RECOVERY_PENDING depois do restart:

```sql
ALTER DATABASE LingolCadastro SET ONLINE;
```

Para conferir o estado: `SELECT name, state_desc FROM sys.databases WHERE name LIKE 'Lingol%'`.

### Tuning aplicado

A causa raiz do travamento era o `max server memory` no padrão (ilimitado): numa máquina de
8 GB o SQL infla até brigar com o Windows pela memória. Limite aplicado:

```sql
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'max server memory (MB)', 1536; RECONFIGURE;
```

No lado da aplicação:

| Ajuste | Por quê |
|---|---|
| `Connect Timeout=60` | O padrão de 15s estourava sob carga e virava um 500 sem explicação |
| `EnableRetryOnFailure(3)` | Absorve falhas transitórias de conexão |
| `AddDbContextPool` | Reaproveita instâncias do DbContext em vez de recriar por request |
| `Microsoft.EntityFrameworkCore.Database.Command: Warning` | Em Development o EF logava **cada query inteira** em nível Information — o log pesava mais que a própria consulta |

Se ainda houver lentidão, nesta ordem:

1. Não rode `ng serve` e o Visual Studio ao mesmo tempo — use `npm start` **ou** o VS, não os dois;
2. Adicione a pasta `...\MSSQL\DATA` às exclusões do Windows Defender (o antivírus varre
   cada escrita em `.mdf`/`.ldf`);
3. Baixe `max server memory` para 1024 MB;
4. Em último caso, troque o SQL Express por **LocalDB** (`Server=(localdb)\MSSQLLocalDB`),
   que sobe sob demanda e libera memória quando ocioso.

As connection strings apontam para `localhost\SQLEXPRESS`, bancos `LingolCadastro` e `LingolPedagogico`.

## Aplicar as migrations

```
dotnet ef database update --project src/Services/Cadastro/Cadastro.Infrastructure/Lingol.Cadastro.Infrastructure --startup-project src/Services/Cadastro/Cadastro.API/Lingol.Cadastro.API --context CadastroDbContext
```

```
dotnet ef database update --project src/Services/Pedagogico/Pedagogico.Infrastructure/Lingol.Pedagogico.Infrastructure --startup-project src/Services/Pedagogico/Pedagogico.API/Lingol.Pedagogico.API --context PedagogicoDbContext
```

## Subir as APIs

```
dotnet run --project src/Services/Cadastro/Cadastro.API/Lingol.Cadastro.API
```

```
dotnet run --project src/Services/Pedagogico/Pedagogico.API/Lingol.Pedagogico.API
```

| Serviço | URL | Swagger |
|---|---|---|
| Cadastro | http://localhost:5030 | /swagger |
| Pedagógico | http://localhost:5209 | /swagger |
| SignalR (atividades) | http://localhost:5209/hubs/atividades | — |

CORS liberado para `http://localhost:4200` (Angular) e `http://localhost:5000` (YARP), configurável em `Cors:Origins`.

## Provedor de IA (Google AI Studio / Gemini)

A chave **nunca** vai para o `appsettings.json` — o repositório é público:

```
dotnet user-secrets set "Gemini:ApiKey" "<token do AI Studio>" --project src/Services/Pedagogico/Pedagogico.API/Lingol.Pedagogico.API
```

Configuração em `appsettings.json` (sem segredo):

```json
"IaProvider": { "UseFake": false },
"Gemini": {
  "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/",
  "Model": "gemini-3.5-flash-lite",
  "TimeoutSegundos": 90,
  "MaxTentativas": 3,
  "Temperatura": 0.7
}
```

Notas:
- `gemini-2.5-flash` **não está disponível para novas contas** (404 com recomendação de migrar).
- Modelo em uso: **`gemini-3.5-flash-lite`**, escolhido por medição (ver tabela abaixo).
- As duas chamadas usam **JSON Mode** (`responseMimeType: "application/json"` + `responseSchema`),
  então o C# desserializa direto, sem parsing de texto livre.
- `IaProvider:UseFake = true` volta ao gerador simulado, útil para testar o fluxo sem gastar cota.
- O Gemini às vezes responde **503 (high demand)**. O cliente já faz retry com backoff
  (`MaxTentativas`), mas em picos longos a atividade termina em `Erro` com a mensagem no
  `mensagemErro` — o professor consegue simplesmente pedir de novo.
- Se a chave não estiver configurada e `UseFake` for `false`, a API falha no startup com mensagem
  explícita — em vez de quebrar só quando o professor clicar em "adaptar".

### Escolha do modelo

Benchmark com o mesmo payload real (JSON Mode + `responseSchema`, 6 questões), 8 rodadas:

| Modelo | Sucesso | Latência média | Observação |
|---|---|---|---|
| `gemini-3.6-flash` | 1/3 | 34,3s | 503 constante |
| `gemini-3.5-flash` | 5/8 | ~20s | ainda dá 503 |
| `gemini-3.1-flash-lite` | 8/8 | 6,8s | devolve `habilidade` como **código da BNCC** (`EF06LP01`) |
| **`gemini-3.5-flash-lite`** | **8/8** | **4,1s** | conceitos por extenso, ideais para o dashboard |

O `habilidade` alimenta o mapa de lacunas do professor, então código da BNCC ali é uma regressão
de produto — foi o critério que desempatou. O prompt também passou a proibir códigos e siglas
nesse campo, como seguro contra qualquer modelo.

Ciclo completo medido após a troca:

| Etapa | Antes (`gemini-3.6-flash`) | Depois (`gemini-3.5-flash-lite`) |
|---|---|---|
| `POST /gerar` → 202 | ~3s | 3,3s |
| Geração das questões | 27s (e falhas 503) | **7,8s** |
| Diagnóstico da entrega | ~50s com retries | **3,3s** |

### Formato das questões por modo

O prompt muda conforme o formato escolhido pelo professor:

| Modo | Enunciado | Alternativas |
|---|---|---|
| `Simples` | Pergunta autoexplicativa | Só o texto da opção |
| `Rpg` | Frase curta com lacuna `________`, ambientada no tema | Só a palavra que preenche |

Em ambos o gabarito é a **letra** (A–D) e a interface numera as opções sozinha.
O `GET /questoes` nunca devolve gabarito nem explicação; eles só chegam ao aluno
pelo `POST /respostas/questao`, depois que ele responde.

O modo `Rpg` aceita no máximo **10 questões** (tamanho do tabuleiro), validado no
`CriarAtividadeCommandHandler`. Temas disponíveis ficam em `TemasAtividade`.

### Duas filas em segundo plano

| Fila | Disparada por | O que faz | Evento SignalR |
|---|---|---|---|
| `GerarAtividadeQueueItem` | `POST /api/atividades/gerar` (202) | Gemini gera as questões, persiste e marca `Pronta` | `AtividadeGerada` / `AtividadeErro` |
| `CorrigirEntregaQueueItem` | `POST /api/atividades/{id}/respostas` | Gemini classifica as dificuldades dos erros e grava `DificuldadesAluno` | `CorrecaoPronta` |

O aluno recebe o placar na hora (correção objetiva, síncrona); o diagnóstico pedagógico roda depois
e alimenta o dashboard do professor.

## Endpoints

### Cadastro (`:5030`)

| Método | Rota | Quem |
|---|---|---|
| POST | `/api/auth/professor/register` \| `/professor/login` \| `/aluno/login` | anônimo |
| POST/GET | `/api/turmas`, `/api/turmas/{id}` | professor |
| POST/GET | `/api/turmas/{id}/alunos` | professor |
| GET | `/api/turmas/minhas` | aluno |
| GET | `/api/alunos/{id}`, `/api/alunos/turma/{id}` | professor / próprio aluno |

### Pedagógico (`:5209`)

| Método | Rota | Quem |
|---|---|---|
| POST | `/api/atividades/gerar` → **202** | professor dono da turma |
| GET | `/api/atividades/turmas/{turmaId}` | professor dono / aluno da turma |
| GET | `/api/atividades/{id}` | professor dono / aluno da turma |
| GET | `/api/atividades/{id}/questoes` (sem gabarito) | professor dono / aluno da turma |
| POST | `/api/atividades/{id}/respostas` | aluno da turma (envio em lote) |
| POST | `/api/atividades/{id}/respostas/questao` | aluno da turma (feedback imediato) |
| POST | `/api/atividades/{id}/finalizar` | aluno da turma |
| GET | `/api/relatorios/turmas/{turmaId}` | professor dono |
| GET | `/api/relatorios/turmas/{turmaId}/alunos/{alunoId}` | professor dono / próprio aluno |
| GET | `/api/relatorios/atividades/{atividadeId}` | professor dono |

O relatório da turma traz o mapa de lacunas por tipo de dificuldade, as 10 questões com maior
percentual de erro (com a habilidade avaliada) e a situação aluno a aluno — **incluindo quem ainda
não entregou**, que é o que o professor precisa para cobrar.

## Roteiro de validação (executado nesta máquina)

| Passo | Resultado |
|---|---|
| Registrar + login do professor | 200, JWT emitido |
| Criar turma (6º ano) | 201 com `ano: 6` |
| Cadastrar aluno com perfil AEE (TDAH) | 201, perfil persistido |
| Login do aluno (nome + matrícula) | 200, JWT com `turmaId` |
| `POST /api/atividades/gerar` (modo Rpg, 10 questões) | **202** imediato |
| Fila + IA | status `Pronta` em ~3s |
| `GET /{id}/questoes` com token do aluno | 10 questões, **sem gabarito nem explicação** |
| `POST /{id}/respostas` (6 certas) | `"Você acertou 6/10 questões!"`, nota 6.0 |
| Entrega duplicada | 400 |
| Aluno tentando gerar atividade | 403 |

### Fase 1 — com o Gemini real

| Passo | Resultado |
|---|---|
| Gerar 10 questões (Morfossintaxe, 6º ano, turma com TDAH) | `Pronta` em ~27s |
| Qualidade | Enunciados curtos, 4 alternativas, tipo por habilidade ("Substantivo próprio") |
| Diagnóstico automático (3/10 acertos) | 7 dificuldades gravadas, todas classificadas como `Gramatica` |
| Feedback ao aluno | Reconhece o acerto antes de apontar o que treinar |

### Fase 2 — relatórios

| Passo | Resultado |
|---|---|
| `GET /api/relatorios/turmas/{id}` | 2 alunos listados, 1 entregou, média da turma, 6 questões críticas com % de erro e habilidade |
| Aluno que não entregou | Aparece no painel com `entregou: false` |
| `GET /api/relatorios/atividades/{id}` | Desempenho questão a questão ("Q1 0% | Concordância verbal com sujeito simples") |
| `GET /api/relatorios/turmas/{id}/alunos/{id}` | Histórico de entregas + feedback da IA + `diagnosticoPronto` |
| Professor B na turma/atividade de outro | 403 |
| Professor B listando atividades de outra turma | 403 |
| Aluno no relatório da turma | 403 |
| Aluno no próprio relatório | 200 |
