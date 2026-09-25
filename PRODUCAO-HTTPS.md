# AESYN — HTTPS em produção (v0.19.1)

A aplicação continua ouvindo HTTP **somente na rede interna** do servidor/contêiner. O TLS termina no Nginx, que encaminha o esquema original para o ASP.NET Core.

## Nginx

Use os cabeçalhos abaixo no bloco `location /`:

```nginx
proxy_pass http://127.0.0.1:10000;
proxy_http_version 1.1;
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
```

O host HTTP deve redirecionar para HTTPS:

```nginx
server {
    listen 80;
    server_name aesyn.com.br www.aesyn.com.br;
    return 301 https://$host$request_uri;
}
```

Depois que o DNS estiver apontando corretamente para a VPS, emita/renove o certificado com Certbot para o domínio AESYN e valide:

```bash
sudo nginx -t
sudo systemctl reload nginx
curl -I https://aesyn.com.br/api/health
```

## Aplicação

A v0.19.1 processa `X-Forwarded-For` e `X-Forwarded-Proto` antes do pipeline HTTPS, habilita HSTS fora de Development e mantém `UseHttpsRedirection()` como proteção adicional.

Nunca exponha diretamente a porta interna da aplicação como endpoint público de produção.


## Web Push / VAPID (v0.19.39)
Em producao, configure somente como variaveis/segredos da VPS:
- `Push__Enabled=true`
- `Push__Subject=mailto:contato@SEU_DOMINIO`
- `Push__PublicKey=<VAPID public key>`
- `Push__PrivateKey=<VAPID private key>`

Nunca grave a chave privada VAPID no repositorio ou ZIP. Em Development o push permanece desligado por padrao.
