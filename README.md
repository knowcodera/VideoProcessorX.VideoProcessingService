# VideoProcessorX.VideoProcessingService

## 📚 Visão Geral  
O **VideoProcessorX.VideoProcessingService** é um microserviço desenvolvido em **.NET 8** para processamento de vídeos, extração de frames e geração de arquivos ZIP. Ele opera de forma assíncrona e escalável, utilizando **RabbitMQ** para mensageria e **FFmpeg** para manipulação de mídia.  

## 🚀 Tecnologias Utilizadas  
- **.NET 8** – Plataforma de desenvolvimento  
- **ASP.NET Core Web API** – Backend da API  
- **Entity Framework Core** – ORM para persistência de dados  
- **RabbitMQ** – Mensageria para eventos assíncronos  
- **FFmpeg** – Processamento e extração de frames de vídeo  
- **Polly** – Resiliência e tratamento de falhas  
- **Docker** – Containerização do serviço  
- **Kubernetes** – Orquestração de containers (planejado)  
- **Terraform** – Infraestrutura como código (planejado)  
- **Azure Kubernetes Service (AKS)** – Implantação em nuvem (planejado)  

## 📁 Estrutura do Projeto  
```bash
VideoProcessingService/
│── docker-compose.yml
│── Dockerfile
│── src/
│   ├── VideoProcessingService.Application/
│   │   ├── Interfaces/  # Contratos de serviços
│   │   ├── Services/  # Implementações de processamento de vídeo
│   ├── VideoProcessingService.Domain/
│   │   ├── Entities/  # Modelos de domínio (Vídeo, Usuário, etc.)
│   │   ├── Interfaces/  # Interfaces de repositórios
│   ├── VideoProcessingService.Infrastructure/
│   │   ├── Data/  # DbContext e configuração do EF Core
│   │   ├── Messaging/  # Implementação do RabbitMQ
│   ├── VideoProcessingService.Presentation/
│   │   ├── Controllers/  # Controladores da API
│   │   ├── Program.cs  # Configuração inicial da API
│── tests/
│   ├── VideoProcessingService.UnitTests/
│   ├── VideoProcessingService.IntegrationTests/
│── README.md
```

## ⚙️ Funcionalidades  
✔ **Upload e processamento de vídeos** com FFmpeg  
✔ **Extração de frames e geração de ZIPs**  
✔ **Mensageria assíncrona com RabbitMQ**  
✔ **Persistência de vídeos no banco de dados**  
✔ **Testes unitários e de integração**  
✔ **Monitoramento e escalabilidade**  

## 🔧 Configuração e Execução  

### 1️⃣ **Pré-requisitos**  
Certifique-se de ter instalado:  
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download)  
- [Docker](https://www.docker.com/)  
- [RabbitMQ](https://www.rabbitmq.com/download.html)  
- [FFmpeg](https://ffmpeg.org/download.html)  

### 2️⃣ **Clonar o Repositório**  
```bash
git clone https://github.com/seu-usuario/VideoProcessorX.VideoProcessingService.git
cd VideoProcessorX.VideoProcessingService
```

### 3️⃣ **Configurar Variáveis de Ambiente**  
Crie um arquivo **appsettings.json** no diretório `VideoProcessingService.Presentation` com o seguinte conteúdo:  
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VideoProcessingDB;User Id=sa;Password=YourPassword;"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "BaseUrl": "http://localhost:8080"
}
```

### 4️⃣ **Rodar a Aplicação**  

#### 🔹 **Localmente (Sem Docker)**  
```bash
dotnet build
dotnet run --project src/VideoProcessingService.Presentation
```

#### 🔹 **Com Docker**  
```bash
docker build -t videoprocessingservice .
docker run -p 8080:8080 -e "ASPNETCORE_ENVIRONMENT=Development" videoprocessingservice
```

#### 🔹 **Com Docker-Compose (Banco de dados + RabbitMQ)**  
```bash
docker-compose up -d
```

## 📌 **Endpoints da API**  

### 🎥 **Processamento de Vídeos**  
| Método | Rota                    | Descrição                                      |
|--------|-------------------------|------------------------------------------------|
| `POST` | `/api/videos/upload`    | Faz upload de um vídeo para processamento      |
| `GET`  | `/api/videos`           | Retorna os vídeos do usuário autenticado      |
| `GET`  | `/api/videos/download/{id}` | Baixa o arquivo ZIP com os frames extraídos |

### ⚡ **Saúde da Aplicação**  
| Método | Rota            | Descrição                        |
|--------|----------------|--------------------------------|
| `GET`  | `/api/health`  | Verifica se a API está online |

## 🤖 **Cobertura de Testes**  
O projeto possui **testes unitários e de integração** cobrindo **80% do código**.  
Os testes incluem:  
✔ Testes de serviços de vídeo (extração de frames)  
✔ Testes de mensageria com RabbitMQ  
✔ Testes de repositórios com banco de dados em memória  

Para executar os testes, utilize:  
```bash
dotnet test
```

## 📜 Licença
Este projeto está sob a licença **MIT**.

---

Feito com ❤️ por Roberto Albano

