# SupermarketAPI

## SQL Server local

Compose inicia SQL Server con la contraseña local predeterminada
`Supermarket2026!Local`. Para cambiarla, copia el archivo de entorno de ejemplo
y modifica `MSSQL_SA_PASSWORD`:

```sh
cp .env.example .env
```

Después, construye e inicia la API y SQL Server juntos:

```sh
docker compose up --build -d --wait
```

La API está disponible en `http://localhost:8080` (Swagger en
`http://localhost:8080/swagger`). La base de datos solo se expone en
`127.0.0.1:1433` y sus datos persisten en el volumen
`supermarket-sqlserver-data`. La API espera a que SQL Server esté listo; luego,
si es necesario, crea `SupermarketDb` y aplica las migraciones pendientes de
Entity Framework Core.

Para iniciar únicamente SQL Server y ejecutar la API fuera de Docker:

```sh
docker compose up -d --wait sqlserver
set -a
. ./.env
set +a
dotnet run --project SupermarketAPI
```

## Publicar la imagen de la API en GitHub Container Registry

Puedes crear una cuenta de GitHub con tu correo electrónico. Durante el
registro, GitHub también te pedirá elegir un nombre de usuario. GHCR usa ese
nombre de usuario (o el nombre de una organización) como propietario de la
imagen; no usa tu correo electrónico. Debes tener permiso para publicar
paquetes con ese propietario.

1. Inicia sesión en GitHub y crea un token de acceso personal clásico en
	**Settings** > **Developer settings** > **Personal access tokens** >
	**Tokens (classic)**. Selecciona el permiso `write:packages` y mantén el
	token en privado.
2. Construye y etiqueta la imagen. Reemplaza `YOUR_GITHUB_USERNAME` por tu
	nombre de usuario de GitHub o por una organización donde tengas permiso
	para publicar paquetes:

	```sh
	docker build -t supermarket-api:latest -f Dockerfile .
	GHCR_OWNER="YOUR_GITHUB_USERNAME"
	IMAGE="ghcr.io/${GHCR_OWNER}/supermarket-api"
	docker tag supermarket-api:latest "${IMAGE}:latest"
	```

3. Inicia sesión en GHCR. Cuando Docker solicite una contraseña, pega el token
	de acceso personal del paso 1, no la contraseña de tu cuenta de GitHub:

	```sh
	docker login ghcr.io -u "$GHCR_OWNER"
	```

4. Publica la imagen:

	```sh
	docker push ghcr.io/bt1141022012/supermarket-api:latest
	```

La imagen publicada estará en
`ghcr.io/YOUR_GITHUB_USERNAME/supermarket-api:latest`. Los paquetes son
privados de forma predeterminada; cambia la visibilidad en la configuración
del paquete de GitHub si quieres que otras personas puedan descargar la imagen.

## Usar la imagen publicada con Docker Compose

En el equipo donde se ejecutarán los contenedores, crea un archivo `.env` con
la contraseña que usarán la API y SQL Server:

```env
MSSQL_SA_PASSWORD=ReemplazaEstoPorUnaClaveFuerte
```

Si el paquete de GHCR es privado, inicia sesión con un usuario que tenga acceso
a la imagen. Cuando Docker solicite la contraseña, introduce un token de GitHub
con permiso `read:packages`:

```sh
docker login ghcr.io -u TU_USUARIO_DE_GITHUB
```

Crea un archivo `compose.yaml` con esta configuración. La API usa la imagen
publicada y comparte la red con SQL Server, como requiere esta configuración de
la aplicación:

```yaml
services:
	api:
		image: ghcr.io/bt1141022012/supermarket-api:latest
		container_name: supermarket-api
		environment:
			ASPNETCORE_ENVIRONMENT: Production
			ConnectionStrings__SupermarketDbConnection: "Server=localhost,1433;Database=SupermarketDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True"
		network_mode: "service:sqlserver"
		depends_on:
			sqlserver:
				condition: service_healthy

	sqlserver:
		image: mcr.microsoft.com/mssql/server:2022-latest
		container_name: supermarket-sqlserver
		environment:
			ACCEPT_EULA: "Y"
			MSSQL_PID: Developer
			MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}
		ports:
			- "127.0.0.1:1433:1433"
			- "127.0.0.1:8080:8080"
		volumes:
			- supermarket-sqlserver-data:/var/opt/mssql
		healthcheck:
			test:
				- CMD-SHELL
				- /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" || exit 1
			interval: 10s
			timeout: 5s
			retries: 10
			start_period: 30s

volumes:
	supermarket-sqlserver-data:
```

Desde esa carpeta, descarga la imagen e inicia ambos contenedores:

```sh
docker compose pull api
docker compose up -d --wait
```

La API quedará disponible en `http://localhost:8080` y Swagger en
`http://localhost:8080/swagger`. Para revisar los contenedores ejecuta
`docker compose ps`; para detenerlos, ejecuta `docker compose down`.
