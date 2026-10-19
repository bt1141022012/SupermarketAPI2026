# SupermarketAPI

## SQL Server local

Compose starts SQL Server with the local default password `Supermarket2026!Local`.
To override it, copy the example environment file and change `MSSQL_SA_PASSWORD`:

```sh
cp .env.example .env
```

Then build and start the API and SQL Server together:

```sh
docker compose up --build -d --wait
```

The API is available at `http://localhost:8080` (Swagger at
`http://localhost:8080/swagger`). The database is exposed only on
`127.0.0.1:1433` and persists in the `supermarket-sqlserver-data` volume.
The API waits for SQL Server to become healthy, then creates `SupermarketDb`
if needed and applies all pending Entity Framework Core migrations.

To run only SQL Server and start the API outside Docker instead:

```sh
docker compose up -d --wait sqlserver
set -a
. ./.env
set +a
dotnet run --project SupermarketAPI
```
