FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos de projeto e restaura dependências
COPY Judoo.slnx .
COPY src/Judoo.Domain/Judoo.Domain.csproj src/Judoo.Domain/
COPY src/Judoo.Application/Judoo.Application.csproj src/Judoo.Application/
COPY src/Judoo.Infrastructure/Judoo.Infrastructure.csproj src/Judoo.Infrastructure/
COPY src/Judoo.Api/Judoo.Api.csproj src/Judoo.Api/

RUN dotnet restore src/Judoo.Api/Judoo.Api.csproj

# Copia todo o código fonte e compila
COPY . .
WORKDIR /app/src/Judoo.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Imagem de Execução em Produção
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Judoo.Api.dll"]
