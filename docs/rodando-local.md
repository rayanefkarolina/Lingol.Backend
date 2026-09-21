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
  "Model": "gemini-3.6-flash",
  "TimeoutSegundos": 90,
  "MaxTentativas": 3,
  "Temperatura": 0.7
}
```

Notas:
- `gemini-2.5-flash` **não está disponível para novas contas** (404 com recomendação de migrar).
  O modelo validado aqui é o `gemini-3.6-flash`.
- As duas chamadas usam **JSON Mode** (`responseMimeType: "application/json"` + `responseSchema`),
  então o C# desserializa direto, sem parsing de texto livre.
- `IaProvider:UseFake = true` volta ao gerador simulado, útil para testar o fluxo sem gastar cota.
- Se a chave não estiver configurada e `UseFake` for `false`, a API falha no startup com mensagem
  explícita — em vez de quebrar só quando o professor clicar em "adaptar".

### Duas filas em segundo plano

| Fila | Disparada por | O que faz | Evento SignalR |
|---|---|---|---|
| `GerarAtividadeQueueItem` | `POST /api/atividades/gerar` (202) | Gemini gera as questões, persiste e marca `Pronta` | `AtividadeGerada` / `AtividadeErro` |
| `CorrigirEntregaQueueItem` | `POST /api/atividades/{id}/respostas` | Gemini classifica as dificuldades dos erros e grava `DificuldadesAluno` | `CorrecaoPronta` |

O aluno recebe o placar na hora (correção objetiva, síncrona); o diagnóstico pedagógico roda depois
e alimenta o dashboard do professor.

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
