# Publicando o Lingol (1 professor + 20 alunos)

Guia de ponta a ponta para quem nunca subiu nada em servidor. Ao final, a turma
acessa um endereço na internet e nada roda mais na sua máquina.

> **Antes de publicar, leia a seção 9 (Segurança).** Há duas chaves que precisam
> ser trocadas.

---

## 1. Como fica montado

| Peça | Onde | Custo |
|---|---|---|
| Frontend (Angular) | Cloudflare Workers — **já está no ar** | grátis |
| API de Cadastro | VM da Oracle Cloud, em contêiner | grátis |
| API Pedagógica | a mesma VM, em contêiner | grátis |
| Banco de produção | PostgreSQL na mesma VM, em contêiner | grátis |
| Banco de desenvolvimento | Neon (PostgreSQL na nuvem) | grátis |
| Certificado HTTPS | Caddy + Let's Encrypt, na VM | grátis |
| Endereço da API | subdomínio no DuckDNS | grátis |

Tudo que é servidor vive numa máquina só. O gateway YARP não vai para produção:
o Caddy faz esse papel.

### Por que o banco de produção não é o Neon

O plano gratuito do Neon cobra por **hora de banco acordado**, e são cerca de 190
por mês. Uma API ligada 24 horas por dia mantém conexão aberta, o que impede o
Neon de hibernar — daria umas 730 horas por mês e o banco seria suspenso.

Na VM não existe essa cota: o Postgres roda ao lado das APIs, com latência de
frações de milissegundo. O Neon fica para o desenvolvimento, onde a aplicação só
roda enquanto você está trabalhando.

---

## 2. O que a aplicação exige

**a) Processo sempre ligado.** A geração de atividade devolve `202` na hora e
joga o trabalho numa fila em memória, consumida por um `BackgroundService`. A
chamada ao Gemini leva de 30 a 90 segundos. Hospedagem que hiberna mata esse
trabalho no meio — por isso uma VM de verdade, e não plano gratuito que dorme.

**b) WebSocket.** O painel do professor usa SignalR.

**c) HTTPS obrigatório.** O frontend está em HTTPS na Cloudflare. Um navegador
recusa chamada para `http://` a partir de página `https://` — é bloqueio de
conteúdo misto, sem contorno possível pelo código. E certificado exige nome de
domínio: não se emite para IP puro. Daí o DuckDNS.

---

## 3. Conta no Neon (banco de desenvolvimento)

Leva cinco minutos e não pede cartão.

1. Acesse <https://neon.tech> e entre com a conta do GitHub.
2. **Create project**:
   - **Name:** `lingol`
   - **Postgres version:** a mais recente oferecida
   - **Region:** a mais próxima do Brasil (normalmente *AWS South America (São Paulo)*)
3. Criado o projeto, o Neon já mostra a **connection string**. Ela tem este
   formato:

   ```
   postgresql://usuario:senha@ep-algo-123456.sa-east-1.aws.neon.tech/neondb?sslmode=require
   ```

4. Você precisa de **dois bancos**. No painel, vá em **Databases → New database**:
   - `lingol_cadastro`
   - `lingol_pedagogico`

   A connection string de cada um é a mesma, trocando só o nome depois da barra.

5. Converta para o formato que o .NET entende. De:

   ```
   postgresql://lingol:SENHA@ep-algo-123456.sa-east-1.aws.neon.tech/lingol_cadastro?sslmode=require
   ```

   para:

   ```
   Host=ep-algo-123456.sa-east-1.aws.neon.tech;Database=lingol_cadastro;Username=lingol;Password=SENHA;SSL Mode=Require
   ```

6. Guarde as duas com `dotnet user-secrets`, **nunca** no `appsettings`. Elas têm
   senha, e o repositório é público:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:Cadastro" "<a do lingol_cadastro>" --project src\Services\Cadastro\Cadastro.API\Lingol.Cadastro.API
   dotnet user-secrets set "ConnectionStrings:Pedagogico" "<a do lingol_pedagogico>" --project src\Services\Pedagogico\Pedagogico.API\Lingol.Pedagogico.API
   ```

7. Crie as tabelas. As ferramentas do EF leem o mesmo `user-secrets` do passo
   anterior, então não precisa repetir a connection string:

   ```powershell
   dotnet ef database update --project src\Services\Cadastro\Cadastro.Infrastructure\Lingol.Cadastro.Infrastructure --startup-project src\Services\Cadastro\Cadastro.API\Lingol.Cadastro.API

   dotnet ef database update --project src\Services\Pedagogico\Pedagogico.Infrastructure\Lingol.Pedagogico.Infrastructure --startup-project src\Services\Pedagogico\Pedagogico.API\Lingol.Pedagogico.API
   ```

   No servidor, onde não existe `user-secrets`, as mesmas ferramentas aceitam a
   variável de ambiente `ConnectionStrings__Cadastro` (ou `__Pedagogico`), que
   tem prioridade sobre o cofre local.

Pronto: dá para rodar tudo na sua máquina sem SQL Server e sem banco instalado.

**Duas coisas que valem saber sobre o Neon.**

O painel oferece dois endereços para o mesmo banco: o direto e o terminado em
`-pooler`. Use o **direto**. O `-pooler` passa por um PgBouncer que não suporta
alguns recursos de sessão, e migration é exatamente o tipo de operação que
tropeça nisso. Com 21 usuários você não precisa de pooler.

E o plano grátis hiberna a compute depois de alguns minutos sem uso. A primeira
consulta depois disso acorda o banco e demora: medi **32 segundos** no primeiro
login e **64 milissegundos** nos seguintes. Não é bug. Se incomodar na
apresentação, faça um login qualquer alguns minutos antes.

---

## 4. Conta na Oracle Cloud (a VM)

O cadastro é o passo mais chato de todo o guia — reserve meia hora.

1. Acesse <https://signup.cloud.oracle.com>.
2. Escolha o país **Brasil** e a região mais próxima (*Brazil East (São Paulo)*
   ou *Brazil Southeast (Vinhedo)*). **A região não pode ser trocada depois.**
3. Preencha os dados e informe um cartão de crédito. É só verificação de
   identidade; a Oracle faz uma cobrança de teste de cerca de US$ 1, estornada em
   seguida. Enquanto a conta ficar em **Always Free**, não há cobrança.
4. Confirme o e-mail e aguarde a liberação (costuma ser minutos, às vezes horas).

### Criando a máquina

1. No painel: **Compute → Instances → Create instance**.
2. **Name:** `lingol`
3. **Image and shape → Edit**:
   - **Image:** Canonical Ubuntu 24.04
   - **Shape → Ampere → VM.Standard.A1.Flex**
   - **OCPUs:** 2 · **Memory:** 12 GB

   > Sobra bastante para o Lingol, e deixa metade da cota gratuita livre para
   > outra máquina no futuro.

4. **Add SSH keys → Generate a key pair for me** e **baixe a chave privada**.
   Sem ela você não entra na máquina, e não dá para baixar depois.
5. **Create**.

### Se aparecer "Out of capacity"

É o erro mais comum da Oracle: as máquinas ARM gratuitas vivem esgotadas nas
regiões concorridas. Opções:

- Tentar de novo em horários diferentes (madrugada costuma funcionar)
- Reduzir para 1 OCPU e 6 GB
- Tentar o outro *availability domain*, se a sua região tiver mais de um

Não adianta insistir de minuto em minuto. Tente algumas vezes por dia.

### Liberando as portas — a armadilha da Oracle

São **duas** camadas de firewall, e quase todo mundo esquece a segunda.

**Primeira, no painel:** na página da instância, clique na *subnet* → *Default
security list* → **Add Ingress Rules**, e crie duas:

| Source CIDR | Protocolo | Porta |
|---|---|---|
| `0.0.0.0/0` | TCP | 80 |
| `0.0.0.0/0` | TCP | 443 |

**Segunda, dentro da máquina:** a imagem Ubuntu da Oracle vem com o `iptables`
bloqueando tudo. Conecte por SSH e rode:

```bash
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

Sem esse segundo passo o site simplesmente não responde, e o sintoma é mudo:
nenhuma mensagem de erro, só uma conexão que nunca completa.

---

## 5. Endereço com HTTPS (DuckDNS)

1. Acesse <https://www.duckdns.org> e entre com GitHub ou Google.
2. No campo de domínio, registre um nome — por exemplo `lingol-api`. Fica
   `lingol-api.duckdns.org`.
3. No campo **current ip**, cole o **IP público** da sua instância Oracle (está
   na página da instância) e clique em **update ip**.

Confirme que funcionou, da sua máquina:

```powershell
nslookup lingol-api.duckdns.org
```

Tem que responder com o IP da VM. Só siga adiante quando responder: o Caddy
precisa desse nome resolvendo para emitir o certificado.

---

## 6. Preparando a máquina

Conecte por SSH (no PowerShell, com a chave que você baixou):

```powershell
ssh -i C:\caminho\para\chave.key ubuntu@SEU-IP
```

Instale o Docker e o Git:

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y docker.io docker-compose-v2 git
sudo usermod -aG docker $USER
```

Saia (`exit`) e conecte de novo — é o que faz valer a permissão do Docker.

Baixe o projeto:

```bash
git clone https://github.com/rayanefkarolina/Lingol.Backend.git
cd Lingol.Backend/deploy
```

---

## 7. Configurando e subindo

```bash
cp .env.exemplo .env
nano .env
```

Preencha os seis valores. Para gerar senha e chave JWT sem inventar na cabeça:

```bash
openssl rand -base64 48
```

| Variável | O que é |
|---|---|
| `DOMINIO` | `lingol-api.duckdns.org` (sem `https://`) |
| `FRONTEND_URL` | o endereço do site na Cloudflare, com `https://` e sem barra no fim |
| `POSTGRES_USER` | `lingol` serve |
| `POSTGRES_PASSWORD` | gerada pelo comando acima |
| `JWT_KEY` | outra, diferente da senha do banco |
| `GEMINI_API_KEY` | a chave **nova** do Google AI Studio |

Salve (`Ctrl+O`, `Enter`, `Ctrl+X`) e suba:

```bash
docker compose up -d --build
```

A primeira vez demora de 5 a 10 minutos: ele compila as duas APIs dentro da
máquina. Acompanhe com:

```bash
docker compose logs -f
```

Quando o Caddy disser `certificate obtained successfully`, o HTTPS está de pé.

### Criando as tabelas em produção

As migrations não rodam sozinhas. Da **sua máquina**, apontando para o banco da
VM — que não está exposto na internet, então o caminho é um túnel SSH:

```powershell
ssh -i C:\caminho\para\chave.key -L 5433:localhost:5432 ubuntu@SEU-IP
```

Deixe essa janela aberta e, em **outra**:

```powershell
$env:ConnectionStrings__Cadastro = "Host=localhost;Port=5433;Database=lingol_cadastro;Username=lingol;Password=<a senha do .env>"
dotnet ef database update --project src\Services\Cadastro\Cadastro.Infrastructure\Lingol.Cadastro.Infrastructure --startup-project src\Services\Cadastro\Cadastro.API\Lingol.Cadastro.API

$env:ConnectionStrings__Pedagogico = "Host=localhost;Port=5433;Database=lingol_pedagogico;Username=lingol;Password=<a senha do .env>"
dotnet ef database update --project src\Services\Pedagogico\Pedagogico.Infrastructure\Lingol.Pedagogico.Infrastructure --startup-project src\Services\Pedagogico\Pedagogico.API\Lingol.Pedagogico.API
```

Reinicie as APIs para pegarem o banco pronto:

```bash
docker compose restart cadastro pedagogico
```

---

## 8. Ligando o frontend

No repositório `Lingol.Project`, edite
`lingol-frontend/src/environments/environment.prod.ts`:

```ts
export const environment = {
  producao: true,
  cadastroApi: 'https://lingol-api.duckdns.org/cadastro',
  pedagogicoApi: 'https://lingol-api.duckdns.org/pedagogico'
};
```

Repare nos prefixos `/cadastro` e `/pedagogico`: as duas APIs moram no mesmo
endereço, separadas por caminho. O Caddy remove o prefixo antes de repassar, e o
SignalR continua funcionando em `/pedagogico/hubs/atividades`.

Faça commit e push — a Cloudflare republica sozinha.

### Teste de aceitação

Pelo site publicado, nesta ordem:

1. Criar conta de professor e entrar
2. Criar uma turma
3. Cadastrar os 20 alunos e anotar as matrículas geradas
4. Gerar um questionário e esperar aparecer *Pronta*
5. Gerar uma atividade gamificada
6. Numa janela anônima, entrar como aluno e responder tudo
7. Conferir a correção e o diagnóstico no painel do professor
8. Confirmar que a atividade ficou verde e travada para aquele aluno
9. Gerar uma revisão individual e ver que só aquele aluno a enxerga

---

## 9. Segurança — antes de publicar

1. **Chave nova do Google AI Studio.** A atual foi digitada em texto puro durante
   o desenvolvimento. Gere outra, apague a antiga, use só no `.env` da VM.
2. **Chave JWT nova**, diferente da de desenvolvimento e diferente da senha do
   banco.
3. **Nada de segredo no Git.** O `.env` está no `.gitignore`; o `.env.exemplo`,
   que é versionado, não tem valor nenhum preenchido.
4. **CORS enxuto:** só a URL real do frontend em `FRONTEND_URL`.
5. **Guarde a chave SSH.** Perdeu, perdeu o acesso à máquina.

---

## 10. Manutenção

**Publicar uma alteração do backend:**

```bash
cd ~/Lingol.Backend && git pull && cd deploy
docker compose up -d --build
```

**Ver o que está acontecendo:**

```bash
docker compose logs -f pedagogico    # ou cadastro, postgres, caddy
docker compose ps                    # o que está de pé
```

**Backup do banco** — nenhum serviço faz isso por você agora:

```bash
docker compose exec postgres pg_dumpall -U lingol > backup-$(date +%F).sql
```

Vale rodar antes de qualquer mudança grande e guardar uma cópia fora da VM.

**Renovação do certificado:** automática, o Caddy cuida.

**IP da VM mudou?** Atualize no DuckDNS. Em instância parada e religada isso pode
acontecer; para evitar, reserve um IP público fixo no painel da Oracle.

---

## 11. Se um dia precisar crescer

A VM gratuita da Oracle aguenta muito mais que uma turma. Antes de pensar em
pagar, os passos naturais são:

- Aumentar OCPUs e memória da instância (a cota Always Free vai até 4 OCPUs e 24 GB)
- Trocar o DuckDNS por um domínio próprio — aí o frontend também sai do
  `.workers.dev` e o endereço inteiro fica com a cara da escola
- Separar o Postgres numa segunda instância gratuita, se o banco crescer
