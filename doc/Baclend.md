# 🧾 API - Control de Gastos Compartidos (tipo mini Splitwise)

## 🎯 Objetivo

Permitir a usuarios crear grupos, registrar gastos y calcular automáticamente deudas entre participantes.

---

## 🧩 Features

### 🔐 Autenticación (JWT)

- Registro de usuario
- Login
- Generación de access token JWT
- Refresh token
- Logout (revocación de refresh token)
- Protección de endpoints

### 👤 Perfil de usuario

- Ver perfil del usuario autenticado
- Actualizar nombre
- Cambiar contraseña

### 👥 Grupos

- Crear grupo
- Listar grupos del usuario (paginado)
- Ver detalle de un grupo
- Eliminar grupo (solo Admin)
- Agregar usuarios al grupo
- Quitar usuarios del grupo

### 💸 Gastos

- Registrar gasto (división equitativa o personalizada)
- Listar gastos por grupo (paginado)
- Ver detalle de un gasto
- Editar gasto
- Eliminar gasto (soft delete)

### 💳 Pagos de deuda

- Registrar pago de deuda entre usuarios
- Listar historial de pagos por grupo

### ⚖️ Balances

- Calcular quién le debe a quién
- Balance neto por usuario
- Simplificación de deudas

### 📄 Paginación

- Listado de gastos paginado
- Listado de grupos paginado
- Listado de pagos paginado

---

## 🧱 Arquitectura

```
src/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── GroupsController.cs
│   ├── ExpensesController.cs
│   ├── PaymentsController.cs
│   └── BalancesController.cs
├── Services/
│   ├── Interfaces/
│   └── Implementations/
│       ├── AuthService.cs
│       ├── UserService.cs
│       ├── GroupService.cs
│       ├── ExpenseService.cs
│       ├── PaymentService.cs
│       └── BalanceService.cs
├── Repositories/
│   ├── Interfaces/
│   └── Implementations/
├── Entities/
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── Middleware/
│   └── JwtMiddleware.cs
└── Helpers/
    └── DebtSimplifier.cs
```

---

## 🗄️ Entidades

### User

| Campo        | Tipo          | Notas        |
| ------------ | ------------- | ------------ |
| Id           | Guid          | PK           |
| Name         | nvarchar(100) |              |
| Email        | nvarchar(255) | Unique       |
| PasswordHash | nvarchar(max) | BCrypt       |
| CreatedAt    | datetime2     |              |
| UpdatedAt    | datetime2     | nullable     |
| IsActive     | bit           | default true |

### Group

| Campo     | Tipo          | Notas         |
| --------- | ------------- | ------------- |
| Id        | Guid          | PK            |
| Name      | nvarchar(100) |               |
| CreatedBy | Guid          | FK → User     |
| CreatedAt | datetime2     |               |
| UpdatedAt | datetime2     | nullable      |
| IsDeleted | bit           | default false |
| DeletedAt | datetime2     | nullable      |

### GroupUser

| Campo     | Tipo         | Notas               |
| --------- | ------------ | ------------------- |
| Id        | Guid         | PK                  |
| UserId    | Guid         | FK → User           |
| GroupId   | Guid         | FK → Group          |
| Role      | nvarchar(20) | Admin / Member      |
| JoinedAt  | datetime2    |                     |
| InvitedBy | Guid         | FK → User, nullable |

### Expense

| Campo       | Tipo          | Notas                               |
| ----------- | ------------- | ----------------------------------- |
| Id          | Guid          | PK                                  |
| Description | nvarchar(255) |                                     |
| Amount      | decimal(18,2) |                                     |
| PaidBy      | Guid          | FK → User (quién pagó físicamente)  |
| CreatedBy   | Guid          | FK → User (quién registró el gasto) |
| GroupId     | Guid          | FK → Group                          |
| CreatedAt   | datetime2     |                                     |
| UpdatedAt   | datetime2     | nullable                            |
| IsDeleted   | bit           | default false                       |
| DeletedAt   | datetime2     | nullable                            |

### ExpenseSplit

| Campo     | Tipo          | Notas         |
| --------- | ------------- | ------------- |
| Id        | Guid          | PK            |
| ExpenseId | Guid          | FK → Expense  |
| UserId    | Guid          | FK → User     |
| Amount    | decimal(18,2) |               |
| IsSettled | bit           | default false |
| SettledAt | datetime2     | nullable      |

### Payment

| Campo      | Tipo          | Notas                    |
| ---------- | ------------- | ------------------------ |
| Id         | Guid          | PK                       |
| FromUserId | Guid          | FK → User (quién paga)   |
| ToUserId   | Guid          | FK → User (quién recibe) |
| GroupId    | Guid          | FK → Group               |
| Amount     | decimal(18,2) |                          |
| Note       | nvarchar(255) | nullable                 |
| CreatedAt  | datetime2     |                          |

### RefreshToken

| Campo     | Tipo          | Notas          |
| --------- | ------------- | -------------- |
| Id        | Guid          | PK             |
| UserId    | Guid          | FK → User      |
| Token     | nvarchar(max) | Hash del token |
| ExpiresAt | datetime2     |                |
| CreatedAt | datetime2     |                |
| IsRevoked | bit           | default false  |

---

## 🔌 Endpoints

### 🔐 Auth

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
```

### 👤 Usuario

```
GET    /api/users/me
PUT    /api/users/me
PUT    /api/users/me/password
```

### 👥 Grupos

```
GET    /api/groups?page=1&pageSize=10
POST   /api/groups
GET    /api/groups/{id}
DELETE /api/groups/{id}
POST   /api/groups/{id}/users
DELETE /api/groups/{id}/users/{userId}
```

### 💸 Gastos

```
POST   /api/expenses
GET    /api/groups/{groupId}/expenses?page=1&pageSize=10
GET    /api/expenses/{id}
PUT    /api/expenses/{id}
DELETE /api/expenses/{id}
```

### 💳 Pagos de deuda

```
POST   /api/groups/{groupId}/payments
GET    /api/groups/{groupId}/payments?page=1&pageSize=10
```

### ⚖️ Balances

```
GET    /api/groups/{groupId}/balances
```

---

## ⚙️ Lógica clave

### División de gastos

- **Equitativa**: `Amount / count(participants)`. Si hay decimales, el centavo restante se asigna al pagador.
- **Personalizada**: El body debe incluir `splits: [{ userId, amount }]`. Validar que `sum(splits) == Amount`.
- El pagador también puede tener su propio `ExpenseSplit` si le corresponde una parte del gasto.

### Cálculo de balances

```
balance(userId) =
  SUM(Expense.Amount WHERE PaidBy = userId AND IsDeleted = false)
  - SUM(ExpenseSplit.Amount WHERE UserId = userId AND Expense.IsDeleted = false)
  + SUM(Payment.Amount WHERE ToUserId = userId)
  - SUM(Payment.Amount WHERE FromUserId = userId)
```

- Resultado positivo → el grupo le debe dinero al usuario.
- Resultado negativo → el usuario le debe dinero al grupo.

### Simplificación de deudas

Algoritmo para minimizar el número de transacciones:

1. Calcular balance neto de cada usuario.
2. Separar en dos listas: acreedores (balance > 0) y deudores (balance < 0).
3. Tomar el mayor deudor y el mayor acreedor.
4. El deudor paga al acreedor el mínimo entre ambos valores absolutos.
5. Reducir ambos balances y repetir hasta que todos sean 0.

---

## 🔐 Seguridad

| Aspecto                  | Implementación                                                                                    |
| ------------------------ | ------------------------------------------------------------------------------------------------- |
| JWT                      | Bearer token en header `Authorization`                                                            |
| Access token expiry      | 15 minutos                                                                                        |
| Refresh token expiry     | 7 días                                                                                            |
| Hashing                  | BCrypt con salt                                                                                   |
| Autorización por recurso | Validar que el usuario autenticado pertenece al grupo en cada request de gastos, pagos y balances |
| Roles en grupo           | Solo el Admin puede eliminar el grupo o quitar miembros                                           |
| Validación de inputs     | FluentValidation en todos los DTOs de request                                                     |

---

## 📦 DTOs de referencia

### POST /api/auth/register — Request

```json
{
  "name": "string",
  "email": "string",
  "password": "string"
}
```

### POST /api/auth/login — Response

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 900
}
```

### POST /api/expenses — Request

```json
{
  "description": "string",
  "amount": 0.0,
  "paidBy": "guid",
  "groupId": "guid",
  "splitType": "equal | custom",
  "splits": [{ "userId": "guid", "amount": 0.0 }]
}
```

### GET /api/groups/{groupId}/balances — Response

```json
{
  "groupId": "guid",
  "balances": [
    {
      "userId": "guid",
      "userName": "string",
      "netBalance": 0.0
    }
  ],
  "simplifiedDebts": [
    {
      "fromUserId": "guid",
      "fromUserName": "string",
      "toUserId": "guid",
      "toUserName": "string",
      "amount": 0.0
    }
  ]
}
```

### POST /api/groups/{groupId}/payments — Request

```json
{
  "fromUserId": "guid",
  "toUserId": "guid",
  "amount": 0.0,
  "note": "string"
}
```

---

## 🚀 Extras opcionales

- Notificaciones push al registrar un gasto
- Exportación de balances a PDF o CSV
- Invitación a grupos por email
