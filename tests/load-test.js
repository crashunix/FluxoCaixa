import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '10s', target: 20 },
        { duration: '30s', target: 50 },  // Carga constante de 50 VUs (Requisito**)
        { duration: '10s', target: 100 }, // Pico de estresse
        { duration: '10s', target: 0 },
    ],
    thresholds: {
        http_req_failed: ['rate<0.01'],        // Menos de 1% de erro
        http_req_duration: ['p(95)<200'],     // p95 abaixo de 200ms
    },
};

export default function () {
    const url = 'http://localhost:5001/transactions';
    const payload = JSON.stringify({
        amount: (Math.random() * 500 + 1).toFixed(2),
        currency: 'BRL',
        transactionType: Math.random() > 0.5 ? 1 : 2,
        description: 'Lançamento automatizado k6',
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    check(res, {
        'status is 201': (r) => r.status === 201,
    });

    sleep(0.05); // Simula requisições rápidas em rajada
}