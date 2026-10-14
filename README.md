# SupermarketAPI

## SQL Server local

Configure a local SQL Server password before starting the database:

```sh
cp .env.example .env
```

Set `MSSQL_SA_PASSWORD` in `.env` to a strong password, then start SQL Server:

```sh
docker compose up -d --wait
```

The database is exposed only on `127.0.0.1:1433` and persists in the
`supermarket-sqlserver-data` volume. The API reads `MSSQL_SA_PASSWORD` from
the environment, so load `.env` before starting it:

```sh
set -a
. ./.env
set +a
dotnet run --project SupermarketAPI
```

On startup, the API creates `SupermarketDb` if needed and applies all pending
Entity Framework Core migrations.
