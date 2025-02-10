import http from 'k6/http';
import { check, sleep } from 'k6';

// 📌 Defina o caminho correto do arquivo localmente
const videoFile = open('./13041678_1920_1080_50fps.mp4', 'b'); 

export let options = {
  stages: [
    { duration: '30s', target: 5 },  // 🚀 Aumenta para 10 usuários simultâneos
    { duration: '1m', target: 10 },  // 📈 Mantém 50 usuários simultâneos por 2 minutos
    { duration: '30s', target: 0 },   // 🔻 Reduz gradualmente para 0 usuários
  ],
  thresholds: {
    http_req_duration: ['p(95)<10000'], // ⏳ 95% das requisições devem ser abaixo de 10s
    http_req_failed: ['rate<0.02'],     // 🔴 Menos de 2% das requisições podem falhar
    http_req_receiving: ['p(95)<3000'], // 🏗️ Tempo de resposta do servidor
    http_req_connecting: ['p(95)<2000'] // 🚦 Tempo de conexão inicial
  },
};

export default function () {
  let url = 'http://localhost:32801/api/Videos/upload'; // 🔗 Endpoint de upload

  // 🔹 Configuração do corpo do request (form-data)
  let formData = {
    File: http.file(videoFile, '13041678_1920_1080_50fps.mp4', 'video/mp4'), 
  };

  // 🔹 Headers com autenticação (substitua o token)
  let params = {
    headers: {
      Authorization: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwidXNlcm5hbWUiOiJSb2JlcnRvIiwiZXhwIjoxNzM5MTU0OTQ3LCJpc3MiOiJBdXRoU2VydmljZUlzc3VlciIsImF1ZCI6IkF1dGhTZXJ2aWNlQXVkaWVuY2UifQ.OqSqNYLtu8SBTrxCsaSH_RMkPxAzT3m00KAlGbR42B0`,  // ⚠️ Insira um token válido

    },
    timeout: '60s' // ⏳ Aumenta timeout para evitar falhas em uploads grandes
  };

  let res = http.post(url, formData, params);

  // ✅ Validações pós-requisição
  let checkRes = check(res, {
    '✅ Status é 200': (r) => r.status === 200,
    '✅ Tempo de resposta < 30s': (r) => r.timings.duration < 30000,
    '🚨 Nenhuma falha crítica': (r) => r.status !== 500 && r.status !== 503,
    '📦 Tamanho do corpo recebido > 0': (r) => r.body.length > 0,
  });

  if (!checkRes) {
    console.error(`❌ Falha no upload: ${res.status} - ${res.body}`);
  }


  sleep(1); // ⏳ Pausa para simular usuários reais
}
