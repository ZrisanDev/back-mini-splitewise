# Splitwise API

Backend API para gestión de gastos compartidos entre grupos de personas, inspirado en Splitwise. Permite crear grupos, registrar gastos con splits (iguales o personalizados), pagos entre usuarios y cálculo automático de balances/deudas simplificadas.

## 🚀 Características

- ✅ **Autenticación JWT** - Login, registro, refresh tokens
- ✅ **Gestión de Grupos** - Crear grupos, agregar usuarios
- ✅ **Gastos compartidos** - Registrar gastos con splits (iguales o personalizados)
- ✅ **Pagos entre usuarios** - Registrar pagos y calcular balances
- ✅ **Cálculo de deudas** - Balance simplificado para minimizar transacciones
- ✅ **Soft Delete** - Eliminación lógica de grupos y gastos
- ✅ **Multi-provider** - Soporta SQL Server, PostgreSQL y SQLite

## 🛠️ Stack Tecnológico

- **.NET 10.0** - Framework principal
- **Entity Framework Core 10.0.6** - ORM
- **Minimal API** - Arquitectura moderna de .NET
- **JWT Bearer** - Autenticación con tokens
- **FluentValidation** - Validación de requests
- **BCrypt.Net-Next** - Hasheo de contraseñas
- **Swagger/OpenAPI** - Documentación de API

## 📋 Requisitos

### Opcionales (se puede usar SQLite sin Docker)
- Docker y Docker Compose para SQL Server
- .NET 10.0 SDK

## 🏁 Estructura del Proyecto

```
back-api-splitwise/
├── Program.cs                          # Entry point y configuración de Minimal API
├── appsettings.json                    # Configuración base (se commitea)
├── appsettings.json.example             # Template de configuración
├── appsettings.Development.json          # Configuración de desarrollo (local)
├── .env                                # Variables de entorno (secretos, NO commitear)
├── docker-compose.yml                    # Configuración de SQL Server en Docker
├── src/
│   ├── Data/
│   │   ├── AppDbContext.cs           # Contexto de EF Core
│   │   └── Configurations/          # Configuraciones de entidades
│   ├── Entities/
│   │   ├── User.cs                  # Usuario
│   │   ├── Group.cs                 # Grupo
│   │   ├── GroupUser.cs             # Relación Usuario-Grupo
│   │   ├── Expense.cs               # Gasto
│   │   ├── ExpenseSplit.cs          # Split de gasto
│   │   ├── Payment.cs               # Pago
│   │   └── RefreshToken.cs          # Token de refresco
│   ├── DTOs/
│   │   ├── Auth/                    # Request/Response de autenticación
│   │   ├── Users/                   # Request/Response de usuarios
│   │   ├── Groups/                  # Request/Response de grupos
│   │   ├── Expenses/                # Request/Response de gastos
│   │   ├── Payments/                # Request/Response de pagos
│   │   ├── Balances/                # Request/Response de balances
│   │   └── Pagination/             # Request de paginación
│   ├── Services/
│   │   ├── AuthService.cs            # Lógica de autenticación
│   │   ├── UserService.cs            # Lógica de usuarios
│   │   ├── GroupService.cs           # Lógica de grupos
│   │   ├── ExpenseService.cs         # Lógica de gastos
│   │   ├── PaymentService.cs         # Lógica de pagos
│   │   ├── BalanceService.cs        # Lógica de balances
│   │   └── Interfaces/             # Interfaces de servicios
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs  # Configuración de DI y EF Core
│   │   └── ExceptionHandlingExtensions.cs   # Manejo global de excepciones
│   ├── Helpers/
│   │   ├── PasswordHasher.cs         # Helper para hashear contraseñas
│   │   └── JwtHelper.cs            # Helper para generar tokens
│   └── Validators/
│       ├── RegisterValidator.cs       # Validador de registro
│       └── LoginValidator.cs          # Validador de login
└── Migrations/
    └── *.cs                            # Migraciones de EF Core
```

## 🚀 Cómo Ejecutar

### Opción 1: Con SQL Server en Docker (Recomendado) 🐳

#### 1. Copiar archivo de configuración
```bash
cp appsettings.json.example appsettings.json
```

#### 2. Crear archivo de variables de entorno
```bash
# Crear .env con tus secretos (NO commitear a git)
cat > .env << 'EOF'
# SQL Server password
MSSQL_SA_PASSWORD=TU_PASSWORD_SEGURO_AQUI
DB_PASSWORD=TU_PASSWORD_SEGURO_AQUI

# JWT Secret Key (mínimo 32 caracteres)
JWT_SECRET=TU_JWT_SECRET_MIN_32_CHARS
EOF
```

#### 3. Iniciar SQL Server con Docker Compose
```bash
docker-compose up -d
```

**Nota**: Primera vez puede tardar ~2-3 minutos en descargar la imagen.

#### 4. Aplicar migraciones
```bash
# Agregar dotnet tools al PATH si es necesario
export PATH="$PATH:$HOME/.dotnet/tools"

# Aplicar migraciones a SQL Server
dotnet ef database update --connection "Server=localhost,1433;Database=splitwise_dev;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=True"
```

#### 5. Ejecutar la API
```bash
dotnet run
```

La API estará disponible en:
- **API**: http://localhost:5180
- **Swagger**: http://localhost:5180/swagger

---

### Opción 2: Con SQLite (Sin Docker) 💾

#### 1. Configurar SQLite
El proyecto está configurado para usar SQLite por defecto si la connection string es "Data Source=app.db".

#### 2. Aplicar migraciones
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update
```

#### 3. Ejecutar la API
```bash
dotnet run
```

---

## 📡 Endpoints de la API

### Autenticación (`/api/auth`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| POST | `/api/auth/register` | Registrar nuevo usuario | ❌ |
| POST | `/api/auth/login` | Login y obtener tokens | ❌ |
| POST | `/api/auth/refresh` | Renovar access token | ❌ |
| POST | `/api/auth/logout` | Revocar refresh token | ✅ |

### Usuarios (`/api/users`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| GET | `/api/users/me` | Obtener perfil del usuario actual | ✅ |
| PUT | `/api/users/me` | Actualizar perfil del usuario actual | ✅ |
| PUT | `/api/users/me/password` | Cambiar contraseña del usuario actual | ✅ |

### Grupos (`/api/groups`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| GET | `/api/groups` | Listar grupos del usuario | ✅ |
| POST | `/api/groups` | Crear nuevo grupo | ✅ |
| GET | `/api/groups/{id}` | Obtener detalle de grupo | ✅ |
| DELETE | `/api/groups/{id}` | Eliminar grupo (soft delete) | ✅ |
| POST | `/api/groups/{id}/users` | Agregar usuario a grupo | ✅ |
| DELETE | `/api/groups/{id}/users/{userId}` | Eliminar usuario de grupo | ✅ |

### Gastos (`/api/expenses`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| POST | `/api/expenses` | Crear nuevo gasto | ✅ |
| GET | `/api/expenses/{id}` | Obtener detalle de gasto | ✅ |
| PUT | `/api/expenses/{id}` | Actualizar gasto | ✅ |
| DELETE | `/api/expenses/{id}` | Eliminar gasto (soft delete) | ✅ |

### Pagos (`/api/groups/{groupId}/payments`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| POST | `/api/groups/{groupId}/payments` | Registrar nuevo pago | ✅ |
| GET | `/api/groups/{groupId}/payments` | Listar pagos del grupo | ✅ |

### Balances (`/api/groups/{groupId}/balances`)
| Método | Endpoint | Descripción | Auth |
|---------|-----------|-------------|-------|
| GET | `/api/groups/{groupId}/balances` | Obtener balances simplificados del grupo | ✅ |

## 🔧 Configuración

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DB_CONNECTION_STRING_FROM_ENV"
  },
  "Jwt": {
    "Issuer": "back-api-splitwise",
    "Audience": "back-api-splitwise-client",
    "SecretKey": "JWT_SECRET_FROM_ENV"
  }
}
```

### Variables de Entorno (.env)
```bash
# SQL Server password
MSSQL_SA_PASSWORD=TU_PASSWORD_SEGURO_AQUI
DB_PASSWORD=TU_PASSWORD_SEGURO_AQUI

# JWT Secret Key (mínimo 32 caracteres)
JWT_SECRET=TU_JWT_SECRET_MIN_32_CHARS
```

### Connection Strings por Provider

**SQL Server**:
```
Server=localhost,1433;Database=splitwise_dev;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

**PostgreSQL**:
```
Host=localhost;Port=5432;Database=splitwise_dev;Username=postgres;Password=YOUR_PASSWORD
```

**SQLite**:
```
Data Source=app.db
```

## 🗄️ Base de Datos

El proyecto soporta 3 proveedores de base de datos mediante detección automática:

### SQL Server (Recomendado para desarrollo)
- Requiere Docker
- Configuración en `docker-compose.yml`
- Connection string debe contener "Server="

### PostgreSQL (Opcional)
- Configuración manual requerida
- Connection string debe contener "Host=" o "Server=" y "Port=5432"

### SQLite (Por defecto para desarrollo rápido)
- No requiere Docker
- Connection string debe ser "Data Source=app.db"
- Base de datos creada automáticamente

## 📝 Ejemplo de Uso

### 1. Registrar usuario
```bash
curl -X POST http://localhost:5180/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Juan Pérez",
    "email": "juan.perez@ejemplo.com",
    "password": "Password123!"
  }'
```

### 2. Login
```bash
curl -X POST http://localhost:5180/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "juan.perez@ejemplo.com",
    "password": "Password123!"
  }'
```

Response:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "nOkojEjzumjz7vnh9DC56N79MWZYuRn3dpKboo02XW4=",
  "expiresIn": 900
}
```

### 3. Acceder a endpoint protegido
```bash
curl -X GET http://localhost:5180/api/users/me \
  -H "Authorization: Bearer TU_ACCESS_TOKEN"
```

## 🧪 Comandos Útiles

```bash
# Ver estado de Docker
docker-compose ps

# Ver logs de SQL Server
docker logs splitwise-sqlserver

# Conectarse a SQL Server
docker exec splitwise-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YOUR_PASSWORD -C -d splitwise_dev

# Ver migraciones
dotnet ef migrations list

# Crear nueva migración
dotnet ef migrations add NombreDeMigracion

# Aplicar migraciones
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update

# Compilar proyecto
dotnet build

# Ejecutar pruebas (cuando las haya)
dotnet test
```

## 🐛 Troubleshooting

### SQL Server no inicia
- Verificar que Docker está corriendo: `docker ps`
- Verificar puerto 1433 no esté en uso: `netstat -tulpn | grep 1433` o `ss -tulpn | grep 1433`
- Primera vez descarga imagen puede tardar ~2-3 minutos

### Error de conexión a BD
- Verificar `.env` tiene `DB_PASSWORD` correcto
- Verificar connection string en `Program.cs` usa variables de entorno
- Verificar Docker container está "healthy": `docker-compose ps`

### Migraciones fallan
- Verificar `dotnet-ef` está instalado: `dotnet ef --version`
- Agregar al PATH: `export PATH="$PATH:$HOME/.dotnet/tools"`
- Especificar connection string: `dotnet ef database update --connection "...`

### API no inicia
- Verificar puerto 5180 no esté en uso: `netstat -tulpn | grep 5180` o `ss -tulpn | grep 5180`
- Verificar logs de errores: `dotnet run` (ver output en consola)

## 📚 Arquitectura

- **Clean Architecture** - Separación por capas (Entities, DTOs, Services)
- **Minimal API** - Arquitectura moderna de .NET, sin controllers tradicionales
- **Dependency Injection** - Inyección de dependencias para servicios
- **Repository Pattern** - EF Core como repositorio
- **Service Layer** - Lógica de negocio en servicios separados
- **Global Exception Handling** - Manejo centralizado de errores

## 🔄 Migraciones

Las migraciones de Entity Framework Core se commitean a GitHub. Son necesarias para:
- Versionar cambios en el esquema de BD
- Permitir a otros devs recrear la BD
- Mantener sincronizado código y BD

**Archivos que se commitean**:
- ✅ `Migrations/*.cs` - Código de migraciones
- ✅ `Migrations/*.Designer.cs` - Snapshots de modelos
- ✅ `AppDbContextModelSnapshot.cs` - Snapshot del modelo actual

**Archivos que NO se commitean**:
- ❌ `.env` - Variables de entorno (secretos)
- ❌ `appsettings.*.json` - Configuración local (Development, etc.)
- ❌ `app.db` - Datos locales de SQLite

## 🔐 Seguridad

- ✅ **Contraseñas hasheadas** con BCrypt
- ✅ **JWT tokens** con expiración (15 minutos)
- ✅ **Refresh tokens** almacenados en BD
- ✅ **Soft Delete** - No se eliminan datos físicamente
- ✅ **Variables de entorno** para secretos
- ⚠️ **HTTPS** - No configurado en desarrollo (configurar en producción)

## 📝 Notas de Desarrollo

- El proyecto usa **Soft Delete** en `Expense` e `Group` (propiedad `IsDeleted`)
- Los balances se calculan usando el **algoritmo de deuda simplificada**
- Los roles de usuario en grupos son: `Admin`, `Member`, `Guest`
- Los gastos se pueden dividir de forma **igual** o **personalizada**
- La API corre en **Development** por defecto con Swagger habilitado

## 🤝 Contribuir

1. Fork el repositorio
2. Crear branch: `git checkout -b feature/tu-feature`
3. Hacer cambios
4. Crear migraciones si modificas el modelo: `dotnet ef migrations add Description`
5. Commitear cambios: `git commit -m "Descripción"`
6. Push: `git push origin feature/tu-feature`
7. Crear Pull Request

## 📜 Licencia

Este proyecto es un portfolio personal para demostrar habilidades en desarrollo backend con .NET.

## 👨‍💻 Autor

[Zrisan](https://github.com/zrisan)

---

**Para más información sobre el proyecto, revisa el código fuente y los comentarios en el código.**
