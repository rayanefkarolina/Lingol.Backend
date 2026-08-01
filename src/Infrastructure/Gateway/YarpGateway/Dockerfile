FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Infrastructure/Gateway/YarpGateway/YarpGateway.csproj", "src/Infrastructure/Gateway/YarpGateway/"]
RUN dotnet restore "src/Infrastructure/Gateway/YarpGateway/YarpGateway.csproj"
COPY . .
WORKDIR "/src/src/Infrastructure/Gateway/YarpGateway"
RUN dotnet publish "YarpGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YarpGateway.dll"]
