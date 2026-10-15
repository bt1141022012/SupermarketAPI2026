# SupermarketAPI

## SQL Server local

Configure a local SQL Server password before starting the database:

```sh
cp .env.example .env
```

Set `MSSQL_SA_PASSWORD` in `.env` to a strong password, then build and start
the API and SQL Server together:

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
