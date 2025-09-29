# [https://sparkify.dev](https://sparkify.dev)

![sparkify_demo](https://github.com/user-attachments/assets/25cb9987-924a-4a7c-806d-ee3b7bfd62c4)

## Overview

An experimental application designed to evaluate the potential of AI in enhancing traditional relational database-centric applications, when provided with the right tool calls.

## Technology Stack

### 1. Client

- **Frontend**: Angular.
- **Styling**: TailwindCSS.

### 2. Cloudflare Tunnel

- **Purpose**: Securely exposes the local development environment to the internet without opening ports on the host machine's router, handles HTTPS termination, and simplifies routing by acting as a single entry point.
- **Deployment**: Runs as a launch daemon on macOS.
- **Configuration**: Managed via `etc/cloudflared/config.yml` and `nginx-proxy.conf`.
- **HTTPS Handling**: Cloudflare is the single point of handling HTTPS, using HTTP/3 (QUIC), in the this platform stack.

### 3. NGINX

- **Role**: Acts as a reverse proxy and load balancer.
- **Deployment**: Runs as a launch daemon on macOS.
- **Configuration**: Custom configurations in `nginx.conf`, `nginx-proxy.conf`, and `proxy.conf.json`.
- **Protocol**: All communication occurs over plain HTTP.

### 4. Docker

- **Orchestration**: Docker Compose is used to manage multiple services.
- **Protocol**: All inter-container communication occurs over plain HTTP.
- **Containers**:
  - **Frontend**: Hosts the Angular application.
  - **Backend API**: .NET API for business logic and data handling.
  - **Database**: PostgreSQL for data storage.
  - **ElasticSearch**: For advanced search capabilities.
  - **Grafana and Prometheus**: For monitoring and observability.
  - **Loki and Tempo**: For log aggregation and tracing.

### 5. Observability

- **OpenTelemetry**: Instrumentation for logging, tracing and metrics.
- **Grafana Stack**: Includes Prometheus, Loki, and Tempo for metrics, logs, and traces.

### 6. Database

- **PostgreSQL**: Managed with custom schemas, migrations, and seed data.

### 7. Deployment

- **Scripts**: Various shell scripts for starting, stopping, and managing services (`start.sh`, `stop.sh`, `restart-api.sh`, etc.).
- **Environment**: Configured via `config.env` for seamless transitions between development and production.

### 8. Miscellaneous

- **AI/ML Integration**: Utilizes cloud-based LLM capabilities, offloading heavy AI/ML processing to OpenAI API services due to hardware constraints.
