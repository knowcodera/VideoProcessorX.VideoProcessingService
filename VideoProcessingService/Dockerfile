# =========================================
# 1) Stage BASE: com aspnet e ffmpeg
# =========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Se a imagem base vem com "USER app" ou "USER $APP_UID",
# precisamos forçar 'root' temporariamente p/ apt-get.
USER root

# Instala ffmpeg no Linux (Debian/Ubuntu-based)
RUN apt-get update && apt-get install -y ffmpeg

# (Opcional) Volta a usar usuário não-root se desejar
# Se preferir, mantenha root. Mas se quiser menor privilégio:
# Você pode criar seu próprio user ou reutilizar $APP_UID
# Exemplo:
# ENV ASPNETCORE_ENVIRONMENT=Production
# USER app
# Se quiser debug do que foi instalado: RUN which ffmpeg && ffmpeg -version

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# =========================================
# 2) Stage BUILD: compila com sdk
# =========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/VideoProcessingService.Presentation/VideoProcessingService.Presentation.csproj", "src/VideoProcessingService.Presentation/"]
COPY ["src/VideoProcessingService.Application/VideoProcessingService.Application.csproj", "src/VideoProcessingService.Application/"]
COPY ["src/VideoProcessingService.Domain/VideoProcessingService.Domain.csproj", "src/VideoProcessingService.Domain/"]
COPY ["src/VideoProcessingService.Infrastructure/VideoProcessingService.Infrastructure.csproj", "src/VideoProcessingService.Infrastructure/"]

RUN dotnet restore "./src/VideoProcessingService.Presentation/VideoProcessingService.Presentation.csproj"

COPY . .

WORKDIR "/src/src/VideoProcessingService.Presentation"
RUN dotnet build "VideoProcessingService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =========================================
# 3) Stage PUBLISH
# =========================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VideoProcessingService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =========================================
# 4) Stage FINAL: usa a imagem base (com ffmpeg)
# =========================================
FROM base AS final
WORKDIR /app

# Copia a pasta publish gerada no stage anterior
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "VideoProcessingService.Presentation.dll"]
