FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto primeiro (para cache de layers)
COPY ["Api/Api.csproj", "Api/"]
COPY ["Aplicacao/Aplicacao.csproj", "Aplicacao/"]
COPY ["Dominio/Dominio.csproj", "Dominio/"]
COPY ["Infraestrutura/Infraestrutura.csproj", "Infraestrutura/"]

# Restaurar dependências
RUN dotnet restore "Api/Api.csproj"

# Copiar todo o código fonte
COPY . .

# Build da aplicação
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Otimizações de runtime
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Api.dll"]