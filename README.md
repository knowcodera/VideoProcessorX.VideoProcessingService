# VideoProcessingService

## 📚 Visão Geral  
O **VideoProcessingService** é um microserviço desenvolvido em **.NET 8** para processamento de vídeos, extração de frames e geração de arquivos ZIP. Ele opera de forma assíncrona e escalável, utilizando **RabbitMQ** para mensageria e **FFmpeg** para manipulação de mídia.  

## 📐 Arquitetura
[Acesse os detalhes da arquitetura](arquitetura.md)


## Video
- Fase 2 - https://youtu.be/JMTzKb7VZ8Q
- Fase 3 - https://youtu.be/M7PkOcWpImw
- Fase 4 - https://youtu.be/H0XOs21J01o
- Hackaton - https://youtu.be/WgBGd7RdiZs (Estava muito nervoso, desculpas por repetir tanto as mesmas coisas)

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

## 🤖 **Cobertura de Testes**  

| Pacote                                      | Cobertura de Linhas | Cobertura de Branches |
|---------------------------------------------|---------------------|-----------------------|
| `VideoProcessingService.Application`       | 33.75%              | 44.44%                |
| `VideoProcessingService.Domain`            | 27.27%              | 100%                  |
| `VideoProcessingService.Infrastructure`    | 7.81%               | 0%                    |
| `VideoProcessingService.Presentation`      | 0%                  | 0%                    |

## 🤖 **Cobertura de Carga**  
![image](https://github.com/user-attachments/assets/7ec4ebd8-20f8-46d5-ae17-2807d9d8d54c)

## 📜 Licença
Este projeto está sob a licença **MIT**.

---

Feito com ❤️ por Roberto Albano

