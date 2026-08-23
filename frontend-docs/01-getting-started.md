# 01 - Getting Started (Frontend)

Welcome to the E-Commerce Backend! This document explains how you, as a frontend developer, can run the backend locally to test your UI against it.

## Prerequisites
1. **Docker & Docker Compose**: Make sure Docker Desktop is installed and running.
2. **.NET 10 SDK**: Required to run the API.

## Running the Backend

1. Open a terminal and navigate to the `backend` folder of this repository:
   ```bash
   cd backend
   ```
2. Start the infrastructure (Database, Redis, RabbitMQ):
   ```bash
   docker compose up -d postgres redis rabbitmq
   ```
3. Run the API:
   ```bash
   dotnet run --project src/ECommerce.API
   ```

The API is now running at **`http://localhost:5139`**.

## Key Interfaces
- **Swagger UI (REST APIs):** [http://localhost:5139/swagger](http://localhost:5139/swagger)
- **GraphQL IDE (Banana Cake Pop):** [http://localhost:5139/graphql](http://localhost:5139/graphql)
- **Hangfire Dashboard (Background Jobs):** [http://localhost:5139/hangfire](http://localhost:5139/hangfire)
