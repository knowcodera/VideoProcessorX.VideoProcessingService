# Caminho: C:\Projetos\VideoProcessorX\VideoProcessorX\VideoProcessorX\Dockerfile

# Estágio de construção
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VideoProcessorX/VideoProcessorX.csproj", "VideoProcessorX/"]
RUN dotnet restore "VideoProcessorX/VideoProcessorX.csproj"
COPY . .
WORKDIR "/src/VideoProcessorX"
RUN dotnet build "VideoProcessorX.csproj" -c Release -o /app/build

# Estágio de publicação
FROM build AS publish
RUN dotnet publish "VideoProcessorX.csproj" -c Release -o /app/publish

# Estágio final com todas as dependências
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instala dependências necessárias
RUN apt-get update && \
    apt-get install -y \
    ffmpeg \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Configura variáveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoProcessorX.dll"]