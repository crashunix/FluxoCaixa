FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/FluxoCaixa.Shared/FluxoCaixa.Shared.csproj", "src/Shared/FluxoCaixa.Shared/"]
COPY ["src/Transactions/FluxoCaixa.Transactions.Domain/FluxoCaixa.Transactions.Domain.csproj", "src/Transactions/FluxoCaixa.Transactions.Domain/"]
COPY ["src/Transactions/FluxoCaixa.Transactions.Application/FluxoCaixa.Transactions.Application.csproj", "src/Transactions/FluxoCaixa.Transactions.Application/"]
COPY ["src/Transactions/FluxoCaixa.Transactions.Infrastructure/FluxoCaixa.Transactions.Infrastructure.csproj", "src/Transactions/FluxoCaixa.Transactions.Infrastructure/"]
COPY ["src/Transactions/FluxoCaixa.Transactions.Api/FluxoCaixa.Transactions.Api.csproj", "src/Transactions/FluxoCaixa.Transactions.Api/"]
RUN dotnet restore "src/Transactions/FluxoCaixa.Transactions.Api/FluxoCaixa.Transactions.Api.csproj"
COPY . .
WORKDIR "/src/src/Transactions/FluxoCaixa.Transactions.Api"
RUN dotnet build "FluxoCaixa.Transactions.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FluxoCaixa.Transactions.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluxoCaixa.Transactions.Api.dll"]
