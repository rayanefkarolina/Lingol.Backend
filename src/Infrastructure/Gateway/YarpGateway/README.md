# YARP Gateway - Lingol

Gateway reverse-proxy mínimo usando YARP que roteia:
- /pedagogico -> API Pedagógico
- /cadastro  -> API Cadastro

Pré-requisitos
- .NET 9 SDK instalado
- As APIs destino (Pedagogico e Cadastro) devidamente executando (por padrão espera-se:
  - Pedagogico: http://localhost:5000/
  - Cadastro:    http://localhost:5001/)

Passo a passo para rodar localmente (PowerShell)

1) Restaurar dependências e build da solução (recomendado na raiz do repo):
   dotnet restore
   dotnet build

2) (Opcional) Adicionar projeto à solution se ainda não estiver:
   dotnet sln add src/Infrastructure/Gateway/YarpGateway/YarpGateway.csproj

3) Ajuste das portas (se necessário):
   - Abra src/Infrastructure/Gateway/YarpGateway/appsettings.json e altere os "Address" das clusters para as portas corretas das suas APIs.

4) Executar o gateway (defina porta do gateway explicitamente):
   # Exemplo: rodar gateway na porta 5002
   $env:ASPNETCORE_URLS = "http://localhost:5002"
   dotnet run --project src/Infrastructure/Gateway/YarpGateway

5) Testar endpoints através do gateway:
   - http://localhost:5002/pedagogico/health  -> roteia para http://localhost:5000/health
   - http://localhost:5002/cadastro/<rota>    -> roteia para http://localhost:5001/<rota>

Executando com Docker

1) Build da imagem (a partir da raiz do repo):
   docker build -t lingol/gateway:local -f src/Infrastructure/Gateway/YarpGateway/Dockerfile .

2) Executar o container (mapeando porta 80 do container para 5002 local):
   docker run -p 5002:80 lingol/gateway:local

3) Teste os mesmos endpoints acima usando a porta 5002.

Observações importantes
- Em produção, garanta TLS (HTTPS), autenticação/autorização no gateway ou antes dele.
- Remova ou proteja qualquer UI de documentação (Swagger) no gateway em produção.
- Configure políticas YARP (timeouts, retries com Polly, circuit-breaker) conforme necessidade de resiliência.

Resolução de problemas
- Se receber erros 502/404, verifique se as APIs destino estão ativas nas portas configuradas.
- Para logs detalhados, ajuste logLevel em appsettings.json ou passe --logging:LogLevel:Default=Debug.

Git / branch (sugestão de comandos para criar branch com as mudanças locais):
   git checkout -b feature/add-yarp-gateway
   git add src/Infrastructure/Gateway/YarpGateway
   git commit -m "feat(gateway): add YARP gateway project"
   git push -u origin feature/add-yarp-gateway

Se quiser que eu gere um arquivo de configuração YARP mais avançado (timeouts, health probes, balanceamento, TLS), diga o que prefere e eu adiciono.
