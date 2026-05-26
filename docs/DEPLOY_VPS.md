# BizAnalytics: deploy on a VPS

This project is ready for a stable VPS deployment with Docker Compose.

## What is included

- `Dockerfile` - builds the React frontend and ASP.NET API into one container
- `docker-compose.prod.yml` - starts the application and PostgreSQL
- `.env.production.example` - production environment variables template

## Recommended target

For a long-running deployment, use a VPS with Docker and persistent storage.

Good fits:

- Hetzner Cloud server docs: https://docs.hetzner.com/cloud/servers/
- Selectel cloud server docs: https://docs.selectel.ru/cloud-servers/about/about-cloud-server/

If you want less server administration, Railway is also viable because it supports long-running services and PostgreSQL:

- Railway deployments: https://docs.railway.com/deployments
- Railway PostgreSQL: https://docs.railway.com/guides/postgresql

## Minimum server recommendation

- Ubuntu 24.04 LTS
- 2 vCPU
- 4 GB RAM
- 40+ GB SSD

## 1. Prepare the server

Install Docker Engine and the Docker Compose plugin:

- Docker Compose install docs: https://docs.docker.com/compose/install/linux/

Open these ports in the server firewall:

- `80/tcp` for the website
- `22/tcp` for SSH

## 2. Upload the project

Clone the repository on the server:

```bash
git clone <YOUR_REPOSITORY_URL> bizanalytics
cd bizanalytics
```

## 3. Create production variables

Copy the template and fill in real values:

```bash
cp .env.production.example .env.production
nano .env.production
```

You must set:

- strong PostgreSQL password
- strong JWT key
- market API keys
- `KGD_PORTAL_TOKEN` for the live KGD registry

## 4. Start the project

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

For the live KGD registry, keep these variables in `.env.production`:

```env
KGD_REGISTRY_MODE=live
KGD_PORTAL_BASE_URL=https://portal.kgd.gov.kz
KGD_PORTAL_TOKEN=your_real_kgd_x_portal_token
```

## 5. Check the deployment

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml ps
docker compose --env-file .env.production -f docker-compose.prod.yml logs -f app
```

Health endpoint:

```bash
curl http://127.0.0.1/health
```

If everything is fine, the site will open at:

```text
http://YOUR_SERVER_IP
```

## 6. Updating the site

```bash
git pull
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

## 7. Important production notes

- Rotate the JWT key and market API keys before public launch.
- Keep `KGD_PORTAL_TOKEN` only on the backend and rotate it if it is exposed.
- Do not keep production secrets in `appsettings.json`.
- PostgreSQL data is stored in the `postgres_data` Docker volume.
- The current compose file publishes the app over HTTP on port `80`.

## 8. HTTPS and domain

For permanent public access, add a domain and place Caddy or Nginx in front of the app.

Caddy automatic HTTPS docs:

- https://caddyserver.com/docs/automatic-https

If you want, the next step can be preparing:

- domain + HTTPS configuration
- automatic GitHub deploy
- server backup strategy
