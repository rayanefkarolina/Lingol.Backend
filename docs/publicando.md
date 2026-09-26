# Publicando o Lingol (1 professor + 20 alunos)

Guia de ponta a ponta para quem nunca subiu nada em servidor. Ao final você terá
três endereços públicos: o site do professor/aluno, a API de Cadastro e a API
Pedagógica.

> **Antes de começar, leia a seção 8 (Segurança).** Tem uma chave de API que
> precisa ser trocada antes de qualquer publicação.

---

## 1. As quatro peças

| Peça | O que é | Onde vai rodar |
|---|---|---|
| Frontend | Angular compilado (HTML/CSS/JS estático, 841 KB) | CDN estático |
| API de Cadastro | ASP.NET, porta 5030 no local | Contêiner sempre ligado |
| API Pedagógica | ASP.NET + SignalR + fila da IA | Contêiner sempre ligado |
| Banco | 2 bancos SQL Server (`LingolCadastro`, `LingolPedagogico`) | Serviço gerenciado |

O gateway YARP **não** precisa ir para produção: o frontend fala direto com as
duas APIs.

---

## 2. O que a aplicação exige (e por que isso elimina várias opções "grátis")

Três características do Lingol restringem a escolha:

**a) A geração de atividade roda em segundo plano, na memória do processo.**
Quando o professor clica em "Adaptar atividade", a API responde `202` na hora e
joga o trabalho numa fila (`System.Threading.Channels`) consumida por um
`BackgroundService`. A chamada ao Gemini leva de 30 a 90 segundos. Se a
hospedagem **desligar o contêiner no meio disso**, a atividade fica presa em
"Processando" para sempre — não há repique automático.

Isso descarta plataformas *serverless* que dormem agressivamente entre
requisições (Cloud Run e Container Apps com escala a zero, por exemplo), a menos
que você aceite esse risco.

**b) O painel do professor usa SignalR**, que precisa de WebSocket. O plano
gratuito do Azure App Service (F1), por exemplo, desabilita WebSocket.

**c) O banco é SQL Server.** Não existe SQL Server gratuito hospedado de forma
robusta. As saídas são: usar o nível gratuito do Azure SQL (compatível, zero
mudança de código) ou migrar para PostgreSQL (grátis com folga, mas exige trocar
o provedor do EF Core e regerar as migrations).

---

## 3. Comparação das opções gratuitas

> Planos gratuitos mudam com frequência. Confirme os limites atuais na página de
> cada serviço antes de decidir.

### Frontend (qualquer um serve, são todos ótimos)

| Serviço | Pontos fortes | Atenção |
|---|---|---|
| **Cloudflare (Workers)** | Banda ilimitada, sem restrição de uso comercial, CDN forte no Brasil | Precisa de um `wrangler.jsonc` no projeto |
| **Vercel** | Configuração mais guiada, detecta Angular sozinha | O plano gratuito (Hobby) é **só para uso não comercial** |
| Netlify | Na prática, igual à Vercel | — |
| Azure Static Web Apps | Fica junto do resto, se você usar Azure | Menos direto |

A escolha entre as duas primeiras não é técnica — as duas servem um SPA estático
igualmente bem, e com 21 usuários nada disso aparece. O que pesa é a licença: o
plano gratuito da Vercel proíbe uso comercial, e uma plataforma escolar é o tipo
de projeto que escorrega para esse lado sem aviso (a escola passa a pagar, sai um
edital, vira produto). O Pages não tem essa cláusula e ainda não limita banda.

### Backend (a parte difícil)

| Serviço | Sempre ligado? | Observações |
|---|---|---|
| **Render (Free)** | ❌ dorme após ~15 min sem tráfego | Aceita Docker, 512 MB RAM, ~50 s para acordar. Horas de execução são compartilhadas entre os serviços gratuitos da conta |
| **Oracle Cloud Always Free** | ✅ nunca dorme | VM de verdade (ARM, até 4 vCPU/24 GB). Grátis sem prazo. Exige Linux e PostgreSQL (SQL Server não roda em ARM) |
| **Fly.io** | ✅ | Muito bom tecnicamente, mas hoje é pago por uso (alguns dólares/mês) |
| Azure Container Apps / Cloud Run | ⚠️ só com escala a zero | O crédito grátis não cobre 1 réplica ligada o mês inteiro |
| Azure App Service F1 | ❌ | Sem WebSocket e com cota de 60 min de CPU por dia |

### Banco

| Serviço | Muda código? | Limite gratuito |
|---|---|---|
| **Azure SQL (oferta gratuita)** | **Não** | ~100.000 vCore-segundos/mês + 32 GB. Pausa sozinho quando ocioso; acordar leva ~1 min. Pede cartão no cadastro |
| **Neon (PostgreSQL)** | Sim | Bem mais folgado (centenas de horas de compute/mês), não pede cartão |
| **Supabase (PostgreSQL)** | Sim | 500 MB; o projeto pausa após 7 dias sem uso |

---

## 4. Caminho recomendado

Para um primeiro deploy, o que economiza mais tempo e evita mexer no código:

```
Frontend  ->  Cloudflare        (Workers, grátis, sem cartão)
APIs      ->  Render            (grátis, Docker, 2 serviços)
Banco     ->  Azure SQL free    (mantém SQL Server, zero mudança de código)
Acordar   ->  cron-job.org      (ping a cada 10 min)
```

**Regra importante:** crie o banco numa região **do mesmo continente** da região
escolhida no Render (ex.: Render *Ohio* + Azure *East US 2*). Cada requisição faz
várias idas e vindas ao banco; se o banco estiver no Brasil e a API nos EUA, tudo
fica lento.

Se você não quiser dar cartão em lugar nenhum, troque o Azure SQL pelo Neon — mas
aí é preciso migrar o EF Core para PostgreSQL antes.

---

## 5. Passo a passo

### Passo 0 — Preparar o código (já feito)

O que foi criado para o deploy:

- `Dockerfile.cadastro` e `Dockerfile.pedagogico`, na raiz do repositório do backend
- `lingol-frontend/src/environments/environment.prod.ts`, já ligado ao build de
  produção pelo `angular.json`
- `lingol-frontend/wrangler.jsonc`, que diz à Cloudflare onde está o site pronto e
  manda devolver o `index.html` em qualquer rota
- `lingol-frontend/.nvmrc` e `.node-version`, fixando o Node 20 no build
- Endpoint `/health` na API de Cadastro — só a Pedagógica tinha

Falta só você colocar as URLs reais no `environment.prod.ts` — isso acontece no
Passo 5.

### Passo 1 — Criar os bancos

1. Crie uma conta em <https://portal.azure.com> (o cadastro pede cartão; a oferta
   gratuita do banco não gera cobrança enquanto estiver dentro do limite).
2. **Create a resource → SQL Database**.
3. Crie um servidor lógico (guarde o usuário e a senha de administrador).
4. Em **Compute + storage**, escolha **Apply free offer** (General Purpose,
   Serverless).
5. Nome do banco: `LingolCadastro`.
6. Repita para `LingolPedagogico` — **no mesmo servidor**, para não gastar duas
   vezes.
7. No servidor criado, vá em **Networking** e ligue *"Allow Azure services and
   resources to access this server"*, e adicione também o **seu IP atual** (é o
   que permite rodar as migrations da sua máquina).
8. Em cada banco, copie a **connection string ADO.NET** e troque
   `{your_password}` pela senha real.

### Passo 2 — Criar as tabelas

Da sua máquina, apontando para a nuvem (PowerShell, na pasta do backend):

```powershell
$env:ConnectionStrings__Cadastro = "<connection string do LingolCadastro>"
dotnet ef database update --project src\Services\Cadastro\Cadastro.Infrastructure\Lingol.Cadastro.Infrastructure --startup-project src\Services\Cadastro\Cadastro.API\Lingol.Cadastro.API
```

```powershell
$env:ConnectionStrings__Pedagogico = "<connection string do LingolPedagogico>"
dotnet ef database update --project src\Services\Pedagogico\Pedagogico.Infrastructure\Lingol.Pedagogico.Infrastructure --startup-project src\Services\Pedagogico\Pedagogico.API\Lingol.Pedagogico.API
```

A primeira chamada pode demorar ~1 min: o banco estava pausado e precisa acordar.

### Passo 3 — Publicar a API de Cadastro

1. Crie conta em <https://render.com> e conecte o GitHub.
2. **New → Web Service →** repositório `Lingol.Backend`.
3. Configure:
   - **Language/Runtime:** Docker
   - **Dockerfile Path:** `Dockerfile.cadastro`
   - **Docker Build Context:** `.` (a raiz)
   - **Instance Type:** Free
   - **Region:** a mesma escolhida para o banco
   - **Health Check Path:** `/health`
4. Em **Environment**, cadastre as variáveis da tabela do Passo 6.
5. **Create Web Service** e acompanhe o log. O primeiro build leva de 5 a 10 min.
6. Anote a URL: algo como `https://lingol-cadastro.onrender.com`.

Teste: abra `https://<sua-url>/health` — deve responder.

### Passo 4 — Publicar a API Pedagógica

Igual ao Passo 3, mudando:
- **Dockerfile Path:** `Dockerfile.pedagogico`
- As variáveis da API Pedagógica (incluindo a chave do Gemini e a URL da API de
  Cadastro publicada no passo anterior)

### Passo 5 — Publicar o frontend

1. No repositório `Lingol.Project`, edite
   `lingol-frontend/src/environments/environment.prod.ts` e cole as duas URLs do
   Render (sem barra no final). Faça commit e push.
2. O arquivo `lingol-frontend/wrangler.jsonc` **já está criado**:

```jsonc
{
  "name": "lingol",
  "compatibility_date": "2026-09-26",
  "assets": {
    "directory": "./dist/lingol-frontend/browser",
    "not_found_handling": "single-page-application"
  }
}
```

   A linha `not_found_handling` é obrigatória. Sem ela, a home abre mas digitar
   `https://seusite/login-aluno` direto na barra de endereço devolve 404 — quem
   resolve a rota é o Angular, dentro do navegador.

   > **Não crie um `public/_redirects` com `/* /index.html 200`.** No Workers essa
   > regra é rejeitada com "infinite loop detected": o servidor já remove `.html` e
   > `/index` sozinho, então ela redirecionaria para si mesma. O
   > `not_found_handling` faz esse papel.

3. Em <https://dash.cloudflare.com>, crie uma conta e vá em
   **Workers & Pages → Create → Connect to Git**, escolhendo o repositório
   `Lingol.Project`. (O Pages foi absorvido pelo Workers; projetos novos caem
   sempre nesse assistente.)
4. Configure:
   - **Nome do projeto:** `lingol`
   - **Build command:** `npm run build`
   - **Deploy command:** `npx wrangler deploy`
   - **Root directory** (em *Advanced settings*): `lingol-frontend`
5. Publique. O endereço sai como `https://lingol.SEU-NOME.workers.dev`.

   O passo a passo detalhado, com as telas do painel, está no guia dedicado à
   Cloudflare.

### Passo 6 — Variáveis de ambiente

No .NET, cada `:` da configuração vira `__` (dois sublinhados) na variável de
ambiente. Cadastre no painel do Render, aba **Environment**:

**API de Cadastro**

| Variável | Valor |
|---|---|
| `ConnectionStrings__Cadastro` | connection string do `LingolCadastro` |
| `Jwt__Key` | **chave nova**, 40+ caracteres aleatórios (não use a do repositório) |
| `Jwt__Issuer` | `Lingol.Auth` |
| `Jwt__Audience` | `Lingol.Client` |
| `Cors__Origins__0` | `https://lingol.SEU-NOME.workers.dev` (a URL real do frontend) |

**API Pedagógica**

| Variável | Valor |
|---|---|
| `ConnectionStrings__Pedagogico` | connection string do `LingolPedagogico` |
| `Jwt__Key` | **exatamente a mesma** da API de Cadastro |
| `Jwt__Issuer` | `Lingol.Auth` |
| `Jwt__Audience` | `Lingol.Client` |
| `Cors__Origins__0` | a URL do frontend |
| `Gemini__ApiKey` | a chave **nova** do Google AI Studio |
| `CadastroService__BaseUrl` | `https://lingol-cadastro.onrender.com/` (com barra no final) |
| `IaProvider__UseFake` | `false` |

As duas APIs precisam da **mesma** `Jwt__Key`: é ela que faz o token emitido no
login ser aceito pela API Pedagógica.

### Passo 7 — Evitar que os serviços durmam

No plano gratuito do Render, o contêiner hiberna após ~15 min sem requisições e
leva ~50 s para voltar. Duas medidas:

1. Crie dois monitores em <https://cron-job.org> (grátis), cada um chamando
   `https://<api>/health` a cada 10 minutos.
2. Ainda assim, **abra o sistema uns 2 minutos antes da aula** e gere as
   atividades com antecedência.

> As horas de execução do plano gratuito são compartilhadas entre os serviços da
> conta. Manter dois serviços acordados 24 h por dia estoura a cota do mês. O
> ajuste prático é deixar o ping só no horário escolar (o cron-job.org permite
> definir a janela de horário).

### Passo 8 — Teste de aceitação antes da aula

Na ordem, pelo site publicado:

1. Criar conta de professor e entrar.
2. Criar uma turma.
3. Cadastrar 20 alunos e **anotar as matrículas geradas**.
4. Gerar uma atividade (questionário) e esperar aparecer "Pronta".
5. Gerar uma atividade gamificada.
6. Numa janela anônima, entrar como aluno com uma matrícula e responder tudo.
7. Conferir no painel do professor se a correção e o diagnóstico apareceram.
8. Confirmar que a atividade ficou verde e travada para aquele aluno.
9. Gerar uma revisão individual pelo painel e conferir que só aquele aluno a vê.

---

## 6. Dá conta de 21 pessoas?

Sim, com folga. O gargalo não é a quantidade de gente:

- **Memória:** cada API usa ~120 MB; o limite do plano gratuito é 512 MB.
- **Simultaneidade:** 20 alunos respondendo geram algumas requisições por
  segundo. Irrelevante.
- **Gemini:** o plano gratuito do AI Studio limita chamadas por minuto e por dia.
  Quem gera atividade é só o professor, algumas vezes por aula — tranquilo.
- **Banco:** o limite da oferta gratuita é tempo de banco *acordado*, não número
  de usuários. Cerca de 55 h/mês com a configuração mínima. Uso escolar de
  2 h/dia, 3 dias por semana, cabe.

O que realmente incomoda é a **espera para acordar** (contêiner ~50 s + banco
~60 s na primeira consulta do dia).

---

## 7. Recuperação automática do que fica pelo caminho

As duas filas (geração de atividade e diagnóstico) vivem na memória do processo.
Se o contêiner hibernar ou for reiniciado no meio de uma geração, aquele trabalho
sumiria e a atividade ficaria presa em "Processando" para sempre.

Isso está resolvido pelo `RetomarProcessamentoService`, na API Pedagógica:

- **Ao subir**, varre o banco e devolve à fila toda atividade em "Pendente" ou
  "Processando" (na inicialização nada pode estar realmente em andamento) e toda
  entrega sem diagnóstico.
- **A cada 10 minutos**, repete a varredura, mas só para o que está parado há
  mais de 15 minutos — pega também o que travou com o processo vivo.
- O pedido original de cada atividade fica guardado em
  `Atividades.PayloadGeracaoJson`, então a retomada reenfileira exatamente o
  mesmo conteúdo, inclusive o foco da revisão individual e o contexto AEE.
- Depois de 3 tentativas sem sucesso, a atividade é marcada como "Erro" com uma
  mensagem clara, em vez de repetir para sempre.
- A varredura nunca derruba a API: se o banco estiver pausado na hora, o erro é
  registrado no log e a tentativa se repete no ciclo seguinte.

Reprocessar é seguro: a geração **substitui** as questões anteriores em vez de
somar a elas, e o diagnóstico ignora entregas já processadas.

> **Pressupõe uma única instância da API**, que é o cenário deste guia. Se um dia
> você rodar duas ou mais, a varredura de inicialização precisa de um tempo de
> carência para não reenfileirar o que a outra instância está processando naquele
> instante — está comentado no código.

Mesmo com a rede de segurança, vale gerar as atividades com antecedência e com a
tela aberta: o SignalR mantém tráfego e segura o contêiner acordado.

---

## 8. Segurança — fazer antes de publicar

1. **Trocar a chave do Google AI Studio.** A chave atual foi digitada em texto
   puro durante o desenvolvimento. Gere uma nova no AI Studio, apague a antiga e
   use a nova **apenas** como variável de ambiente `Gemini__ApiKey`.
2. **Trocar a chave JWT.** A que está no `appsettings.json` é de
   desenvolvimento e está num repositório público. Em produção, use uma chave
   nova, só na variável de ambiente.
3. **Nunca commitar segredo nenhum.** Os dois repositórios são públicos no
   GitHub. Connection string, chave de IA e chave JWT ficam só no painel da
   hospedagem.
4. **Revisar o CORS.** Deixe apenas a URL real do frontend em `Cors__Origins__0`.

---

## 9. Se quiser algo mais sólido

O gratuito resolve uma turma piloto. Para uso contínuo em escola, o degrau
seguinte é pequeno:

- **VPS simples** (Hetzner, Contabo, DigitalOcean): de 4 a 6 dólares por mês,
  máquina sempre ligada, tudo num lugar só — as duas APIs, o banco e o frontend,
  com Docker Compose e HTTPS automático via Caddy. Sem hibernação, sem cota.
- **Oracle Cloud Always Free:** a mesma ideia, de graça e sem prazo, com a
  ressalva de exigir PostgreSQL e um pouco mais de trabalho inicial.

Em qualquer um dos dois, os `Dockerfile.*` criados aqui continuam valendo.

---

## Limpeza já feita

O repositório carregava, desde o commit inicial, uma pasta fantasma em
`src/Services/Cadastro/Cadastro.Infrastructure/Cadastro.API/`. Dentro dela havia
um único arquivo, `Lingol.Cadastro.Infrastructure.csproj.EntityFrameworkCore.targets`,
num caminho em que até `Lingol.Cadastro.API.csproj` era um **diretório**, não um
projeto.

É lixo de ferramenta: quando o `dotnet ef` recebe em `--project` um caminho que
não existe, ele cria a árvore de pastas, escreve esse `.targets` temporário e não
limpa depois. A solução nunca referenciou nada ali.

A pasta foi removida e o `.gitignore` ganhou a regra
`*.EntityFrameworkCore.targets` para o arquivo não voltar.
