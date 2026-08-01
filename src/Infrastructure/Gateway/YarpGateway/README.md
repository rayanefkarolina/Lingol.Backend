# YARP Gateway - Lingol

Este projeto é um gateway reverse-proxy simples baseado em YARP para rotear /pedagogico para a API pedagógica e /cadastro para a API de cadastro.

Como usar (local):
1. Ajuste as portas em appsettings.json se necessário (destination Address).
2. Build e run via dotnet:
   dotnet build
   dotnet run --project src/Infrastructure/Gateway/YarpGateway

Teste:
- Acesse http://localhost:5002/pedagogico/health para rotear para a API pedagógica (supondo que a API esteja em localhost:5000).
- Acesse http://localhost:5002/cadastro/... para o serviço de cadastro.

Docker:
- docker build -t lingol/gateway:local -f src/Infrastructure/Gateway/YarpGateway/Dockerfile .
- docker run -p 80:80 lingol/gateway:local

Notas:
- Em produção, configure TLS e autenticação no gateway ou antes dele.
- Esta implementação é mínima; ajuste políticas de timeout, retries e cabeçalhos conforme necessidade.
