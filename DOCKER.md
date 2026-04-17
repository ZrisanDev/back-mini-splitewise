# Docker Compose - SQL Server

## Uso

### Levantar SQL Server

```bash
docker-compose up -d
```

Esto iniciará SQL Server 2022 en:
- **Host**: `localhost`
- **Port**: `1433`
- **User**: `sa`
- **Password**: `YourStrong!Passw0rd`
- **Database**: Creado por EF Core migrations

### Verificar que SQL Server está corriendo

```bash
docker-compose ps
```

Deberías ver `splitwise-sqlserver` con status `Up`.

### Conectar con SQL Server Management Studio (opcional)

Si querés conectar con SSMS u otra herramienta:

```
Server: localhost,1433
Authentication: SQL Server Authentication
User: sa
Password: YourStrong!Passw0rd
```

### Detener SQL Server

```bash
docker-compose down
```

### Ver logs

```bash
docker-compose logs -f sqlserver
```

## Configuración de la API

### Para usar con SQL Server en Docker

Copia el contenido de `appsettings.Docker.json` a tu `appsettings.json` local:

```bash
cp appsettings.Docker.json appsettings.json
```

O seteá la variable de entorno `ASPNETCORE_ENVIRONMENT=Docker` cuando corras la app.

### Crear database con migrations

```bash
# Aseguráte que SQL Server está corriendo
docker-compose up -d

# Crear initial migration (si no existe)
dotnet ef migrations add InitialCreate

# Aplicar migration a SQL Server
dotnet ef database update
```

### Correr la app con SQL Server

```bash
# Opción 1: Usar appsettings.Docker.json
export ASPNETCORE_ENVIRONMENT=Docker
dotnet run

# Opción 2: Usar appsettings.json local (que ya tiene la connection string de SQL Server)
dotnet run
```

## Connection String

La connection string para SQL Server está en `appsettings.Docker.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=splitwise_dev;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  }
}
```

**Nota importante**:
- `TrustServerCertificate=True` es necesario para conexiones self-signed (Docker)
- El puerto `,1433` es el formato correcto para SQL Server
- `Database=splitwise_dev` se creará automáticamente si no existe

## Troubleshooting

### SQL Server no inicia

```bash
# Ver logs
docker-compose logs sqlserver

# Verificar puerto 1433 no está en uso
netstat -an | grep 1433
```

### Error de conexión desde la app

Si recibís error de conexión:

1. **Verificar que SQL Server está corriendo**: `docker-compose ps`
2. **Verificar healthcheck**: `docker-compose logs` debería mostrar "SQL Server is now ready for client connections"
3. **Esperar unos segundos**: SQL Server puede tardar 10-15s en iniciarse
4. **Verificar connection string**: Puerto 1433, password correcta

### Migration falla

Si `dotnet ef database update` falla:

1. **Asegurarte que SQL Server está corriendo**
2. **Verificar que la connection string sea correcta**
3. **Intentar recrear el container**:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

## Persistencia de datos

Los datos de SQL Server se guardan en un volumen Docker:
- `sqlserver_data` volumen

Para borrar todo y empezar de cero:

```bash
docker-compose down -v
docker-compose up -d
```

Esto borrará el volumen y todos los datos.
