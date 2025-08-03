FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
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
RUN dotnet publish "Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]