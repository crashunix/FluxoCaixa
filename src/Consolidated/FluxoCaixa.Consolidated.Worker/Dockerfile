FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/FluxoCaixa.Shared/FluxoCaixa.Shared.csproj", "src/Shared/FluxoCaixa.Shared/"]
COPY ["src/Consolidated/FluxoCaixa.Consolidated.Domain/FluxoCaixa.Consolidated.Domain.csproj", "src/Consolidated/FluxoCaixa.Consolidated.Domain/"]
COPY ["src/Consolidated/FluxoCaixa.Consolidated.Infrastructure/FluxoCaixa.Consolidated.Infrastructure.csproj", "src/Consolidated/FluxoCaixa.Consolidated.Infrastructure/"]
COPY ["src/Consolidated/FluxoCaixa.Consolidated.Worker/FluxoCaixa.Consolidated.Worker.csproj", "src/Consolidated/FluxoCaixa.Consolidated.Worker/"]
RUN dotnet restore "src/Consolidated/FluxoCaixa.Consolidated.Worker/FluxoCaixa.Consolidated.Worker.csproj"
COPY . .
WORKDIR "/src/src/Consolidated/FluxoCaixa.Consolidated.Worker"
RUN dotnet build "FluxoCaixa.Consolidated.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FluxoCaixa.Consolidated.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluxoCaixa.Consolidated.Worker.dll"]
