# Problemas y Soluciones

## Problemas Identificados

### 1. Typo en variable de entorno
**Error**: `ASPONETCORE_ENVIROMENT`
**Correcto**: `ASPNETCORE_ENVIRONMENT`

### 2. launchSettings.json tiene precedencia
**Problema**: La app está leyendo `launchSettings.json` en lugar de respetar la variable de entorno
**Solución**: Eliminar o modificar `launchSettings.json`

### 3. 404 en http://localhost:5180/
**Problema**: No hay ruta "/" configurada - todos los endpoints están bajo `/api/...`
**Solución**: Swagger está en `/swagger`

## Soluciones Aplicadas

### 1. Archivo .env creado para Docker Compose

```bash
# Variables en .env (NO commitear a git)
MSSQL_SA_PASSWORD=YourStrong!Passw0rd
```

### 2. docker-compose.yml actualizado para usar .env

```yaml
environment:
  MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:-YourStrong!Passw0rd}
```

### 3. .gitignore actualizado

```gitignore
.env
app.db
appsettings.*.json
```

---

## Cómo Corregir

### Paso 1: Verificar launchSettings.json

El `launchSettings.json` está forzando `Development` y no respeta la variable de entorno.

**Opción A**: Eliminar launchSettings.json (recomendado)
```bash
rm Properties/launchSettings.json
```

**Opción B**: Modificar launchSettings.json para respeter ASPNETCORE_ENVIRONMENT
```json
{
  "profiles": {
    "Docker": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Docker"
      }
    }
  }
}
```

### Paso 2: Corregir el typo de variable

```bash
# ❌ INCORRECTO
export ASPONETCORE_ENVIROMENT=Docker

# ✅ CORRECTO
export ASPNETCORE_ENVIRONMENT=Docker
```

### Paso 3: Crear .env (si no existe)

```bash
cp .env.example .env
```

### Paso 4: Levantar Docker Compose con .env

```bash
docker-compose up -d
```

### Paso 5: Correr la app

```bash
# Usar entorno Docker (SQL Server)
export ASPNETCORE_ENVIRONMENT=Docker
dotnet run
```

### Paso 6: Abrir Swagger

```
http://localhost:5180/swagger
```

---

## Verificación

### Verificar que SQL Server está corriendo

```bash
docker-compose ps
```

Deberías ver `splitwise-sqlserver` con status `Up`.

### Verificar logs de SQL Server

```bash
docker-compose logs sqlserver
```

Deberías ver "SQL Server is now ready for client connections".

### Verificar en qué entorno corre la app

```bash
# La app debería imprimir en logs:
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Docker
```

---

## Troubleshooting

### Error: launchSettings.json no respeta ASPNETCORE_ENVIRONMENT

**Razón**: ASP.NET Core da precedencia a launchSettings.json sobre variables de entorno.

**Solución**: Eliminar el archivo:
```bash
rm Properties/launchSettings.json
```

### Error: Cannot connect to SQL Server

**Razón 1**: SQL Server no está listo
**Solución**: Esperar 15-20 segundos después de `docker-compose up -d`

**Razón 2**: Puerto 1433 en uso
**Solución**: Verificar con `netstat -an | grep 1433` y cambiar puerto en docker-compose.yml

**Razón 3**: Connection string incorrecta
**Solución**: Verificar que `appsettings.Docker.json` tiene la connection string correcta

### Error: 404 al abrir http://localhost:5180/

**Razón**: No hay endpoint "/" configurado
**Solución**: Abrir `/swagger` en su lugar:
```
http://localhost:5180/swagger
```

---

## Quick Start (Sin launchSettings.json)

```bash
# 1. Eliminar launchSettings.json
rm Properties/launchSettings.json

# 2. Levantar SQL Server
docker-compose up -d

# 3. Esperar 15s
sleep 15

# 4. Correr app con SQL Server
export ASPNETCORE_ENVIRONMENT=Docker
dotnet run

# 5. Abrir Swagger en el navegador
# http://localhost:5180/swagger
```

---

## Quick Start (Con launchSettings.json modificado)

Si preferís mantener launchSettings.json, modificá el perfil "Docker":

```bash
# 1. Modificar launchSettings.json para agregar perfil Docker
# (ver "Opción B" arriba)

# 2. Levantar SQL Server
docker-compose up -d

# 3. Esperar 15s
sleep 15

# 4. Correr app usando el perfil Docker
dotnet run --launch-profile Docker

# 5. Abrir Swagger
# http://localhost:5180/swagger
```
