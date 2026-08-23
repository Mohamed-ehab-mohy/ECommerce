# 02 - API Reference

## REST APIs
The primary way to interact with the backend is via REST.
You can explore and test all available endpoints using **Swagger UI**:
[http://localhost:5139/swagger](http://localhost:5139/swagger)

**Core Modules:**
- `Catalog`: `/api/v1/products`, `/api/v1/categories`, `/api/v1/brands`
- `Cart`: `/api/v1/carts/me`
- `Checkout & Orders`: `/api/v1/checkouts`, `/api/v1/orders`
- `Wallets`: `/api/v1/wallets/me`

## GraphQL API
For complex queries (specifically for the Catalog), you can use the GraphQL endpoint:
**Endpoint:** `http://localhost:5139/graphql`

You can explore the schema and test queries using the **Banana Cake Pop UI** by visiting the URL in your browser.

## Authentication
The application uses **JWT Bearer Authentication**.

1. Obtain a token by calling the appropriate `/api/v1/auth/` endpoint.
2. In all subsequent requests, include the token in the headers:
   ```http
   Authorization: Bearer <your-jwt-token>
   ```

## Pagination & Filtering
List endpoints generally support cursor-based or offset-based pagination and standard filtering via query parameters. Refer to Swagger for exact models.
