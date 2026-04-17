# Splitwise API — Guía de Testing Manual

## Prerrequisitos

```bash
dotnet run
```

Abrir Swagger UI: `http://localhost:<port>/swagger`

> Los endpoints marcados con 🔓 son públicos (AllowAnonymous).  
> Los endpoints marcados con 🔒 requieren JWT Bearer token (RequireAuthorization).

---

## Test Flow 1: Flujo Completo de Usuario (Full Journey)

### Paso 1 — Registrar 3 usuarios

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 1 | POST | `/api/auth/register` | 🔓 | `{ "name": "Alice", "email": "alice@test.com", "password": "password123" }` |
| 2 | POST | `/api/auth/register` | 🔓 | `{ "name": "Bob", "email": "bob@test.com", "password": "password123" }` |
| 3 | POST | `/api/auth/register` | 🔓 | `{ "name": "Charlie", "email": "charlie@test.com", "password": "password123" }` |

**Resultado esperado:** 201 Created para cada uno. Anotar los `id` de cada usuario.

### Paso 2 — Login como cada usuario

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 4 | POST | `/api/auth/login` | 🔓 | `{ "email": "alice@test.com", "password": "password123" }` |
| 5 | POST | `/api/auth/login` | 🔓 | `{ "email": "bob@test.com", "password": "password123" }` |
| 6 | POST | `/api/auth/login` | 🔓 | `{ "email": "charlie@test.com", "password": "password123" }` |

**Resultado esperado:** 200 OK con `{ "accessToken": "...", "refreshToken": "...", "expiresIn": 900 }`.  
Copiar los `accessToken` de Alice, Bob y Charlie.

### Paso 3 — Crear grupo (Alice como Admin)

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 7 | POST | `/api/groups` | 🔒 Alice | `{ "name": "Casa Compartida" }` |

**Resultado esperado:** 201 Created. Anotar el `id` del grupo → `{groupId}`.

### Paso 4 — Agregar Bob y Charlie al grupo

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 8 | POST | `/api/groups/{groupId}/users` | 🔒 Alice | `{ "userId": "<bobId>", "role": "Member" }` |
| 9 | POST | `/api/groups/{groupId}/users` | 🔒 Alice | `{ "userId": "<charlieId>", "role": "Member" }` |

**Resultado esperado:** 204 No Content.

### Paso 5 — Crear gasto (Alice paga $30, split igual)

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 10 | POST | `/api/expenses` | 🔒 Alice | Ver abajo |

```json
{
  "description": "Cena juntos",
  "amount": 30.00,
  "paidBy": "<aliceId>",
  "groupId": "<groupId>",
  "createdBy": "<aliceId>",
  "splitType": "equal"
}
```

**Resultado esperado:** 201 Created. El gasto debe tener 3 splits de $10.00 cada uno.

### Paso 6 — Ver gastos del grupo

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 11 | GET | `/api/groups/{groupId}/expenses` | 🔒 Alice |

**Resultado esperado:** 200 OK con `items` conteniendo 1 gasto ("Cena juntos", $30.00).

### Paso 7 — Verificar balances

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 12 | GET | `/api/groups/{groupId}/balances` | 🔒 Alice |

**Resultado esperado:** 200 OK:
```json
{
  "groupId": "<groupId>",
  "balances": [
    { "userId": "<aliceId>", "userName": "Alice", "netBalance": 20.00 },
    { "userId": "<bobId>", "userName": "Bob", "netBalance": -10.00 },
    { "userId": "<charlieId>", "userName": "Charlie", "netBalance": -10.00 }
  ],
  "simplifiedDebts": [
    { "fromUserId": "<bobId>", "toUserId": "<aliceId>", "amount": 10.00 },
    { "fromUserId": "<charlieId>", "toUserId": "<aliceId>", "amount": 10.00 }
  ]
}
```

> Alice pagó $30, le deben $20 (ella absorbe $10 de su parte).  
> Bob debe $10. Charlie debe $10.

### Paso 8 — Bob paga $10 a Alice

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 13 | POST | `/api/groups/{groupId}/payments` | 🔒 Bob | `{ "fromUserId": "<bobId>", "toUserId": "<aliceId>", "amount": 10.00, "note": "Pago cena" }` |

**Resultado esperado:** 201 Created.

### Paso 9 — Verificar balances actualizados

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 14 | GET | `/api/groups/{groupId}/balances` | 🔒 Alice |

**Resultado esperado:**
```json
{
  "balances": [
    { "userId": "<aliceId>", "userName": "Alice", "netBalance": 10.00 },
    { "userId": "<bobId>", "userName": "Bob", "netBalance": 0.00 },
    { "userId": "<charlieId>", "userName": "Charlie", "netBalance": -10.00 }
  ],
  "simplifiedDebts": [
    { "fromUserId": "<charlieId>", "toUserId": "<aliceId>", "amount": 10.00 }
  ]
}
```

> Bob saldó su deuda. Charlie aún debe $10.

### Paso 10 — Crear otro gasto (Charlie paga $60, split igual)

| # | Method | Endpoint | Auth | Body |
|---|--------|----------|------|------|
| 15 | POST | `/api/expenses` | 🔒 Charlie | `{ "description": "Supermercado", "amount": 60.00, "paidBy": "<charlieId>", "groupId": "<groupId>", "createdBy": "<charlieId>", "splitType": "equal" }` |

**Resultado esperado:** 201 Created con 3 splits de $20.00.

### Paso 11 — Verificar balances finales

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 16 | GET | `/api/groups/{groupId}/balances` | 🔒 Alice |

**Resultado esperado:**
- Alice: $10 (+$10) = $10 → le deben $10
- Bob: $0 (-$20) = -$20 → debe $20
- Charlie: -$10 (+$20) = +$10 → le deben $10

```json
{
  "balances": [
    { "userId": "<aliceId>", "userName": "Alice", "netBalance": 10.00 },
    { "userId": "<charlieId>", "userName": "Charlie", "netBalance": 10.00 },
    { "userId": "<bobId>", "userName": "Bob", "netBalance": -20.00 }
  ],
  "simplifiedDebts": [
    { "fromUserId": "<bobId>", "toUserId": "<aliceId>", "amount": 10.00 },
    { "fromUserId": "<bobId>", "toUserId": "<charlieId>", "amount": 10.00 }
  ]
}
```

---

## Test Flow 2: Paginación

### Paso 1 — Crear múltiples gastos en un grupo

Crear 25+ gastos usando el grupo existente (cualquier miembro puede crear):

```bash
for i in $(seq 1 25); do
  curl -X POST "http://localhost:<port>/api/expenses" \
    -H "Authorization: Bearer <aliceToken>" \
    -H "Content-Type: application/json" \
    -d "{\"description\":\"Gasto $i\",\"amount\":10.00,\"paidBy\":\"<aliceId>\",\"groupId\":\"<groupId>\",\"createdBy\":\"<aliceId>\",\"splitType\":\"equal\"}"
done
```

### Paso 2 — Verificar primera página

| # | Method | Endpoint | Auth | Query |
|---|--------|----------|------|-------|
| 1 | GET | `/api/groups/{groupId}/expenses` | 🔒 Alice | `?page=1&pageSize=10` |

**Resultado esperado:** 200 OK con:
- `items`: 10 gastos
- `page`: 1
- `pageSize`: 10
- `totalCount`: ≥ 26
- `totalPages`: ≥ 3

### Paso 3 — Verificar segunda página

| # | Method | Endpoint | Auth | Query |
|---|--------|----------|------|-------|
| 2 | GET | `/api/groups/{groupId}/expenses` | 🔒 Alice | `?page=2&pageSize=10` |

**Resultado esperado:** 200 OK con 10 gastos diferentes a la página 1.

### Paso 4 — Verificar página con resultados parciales

| # | Method | Endpoint | Auth | Query |
|---|--------|----------|------|-------|
| 3 | GET | `/api/groups/{groupId}/expenses` | 🔒 Alice | `?page=3&pageSize=10` |

**Resultado esperado:** 200 OK con los gastos restantes (< 10 items).

### Paso 5 — Verificar página fuera de rango

| # | Method | Endpoint | Auth | Query |
|---|--------|----------|------|-------|
| 4 | GET | `/api/groups/{groupId}/expenses` | 🔒 Alice | `?page=999&pageSize=10` |

**Resultado esperado:** 200 OK con `items: []` (lista vacía).

---

## Test Flow 3: Refresh Token

### Paso 1 — Login

| # | Method | Endpoint | Body |
|---|--------|----------|------|
| 1 | POST | `/api/auth/login` | `{ "email": "alice@test.com", "password": "password123" }` |

**Resultado esperado:** 200 OK con `accessToken` (15 min) y `refreshToken` (7 días).

### Paso 2 — Usar access token (debe funcionar)

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 2 | GET | `/api/users/me` | 🔒 Token de Alice |

**Resultado esperado:** 200 OK con datos de Alice.

### Paso 3 — Refresh token

| # | Method | Endpoint | Body |
|---|--------|----------|------|
| 3 | POST | `/api/auth/refresh` | `{ "refreshToken": "<refreshTokenDeAlice>" }` |

**Resultado esperado:** 200 OK con NUEVO `accessToken` y NUEVO `refreshToken`.

### Paso 4 — Verificar que el nuevo token funciona

| # | Method | Endpoint | Auth |
|---|--------|----------|------|
| 4 | GET | `/api/users/me` | 🔒 Nuevo Token de Alice |

**Resultado esperado:** 200 OK.

### Paso 5 — Logout

| # | Method | Endpoint | Body |
|---|--------|----------|------|
| 5 | POST | `/api/auth/logout` | `{ "refreshToken": "<refreshTokenAnteriorOActual>" }` |

**Resultado esperado:** 204 No Content.

### Paso 6 — Intentar usar refresh token revocado

| # | Method | Endpoint | Body |
|---|--------|----------|------|
| 6 | POST | `/api/auth/refresh` | `{ "refreshToken": "<refreshTokenRevocado>" }` |

**Resultado esperado:** 400 Bad Request (token revocado o no encontrado).

---

## Test Flow 4: Casos de Error

| # | Escenario | Method | Endpoint | Body / Nota | Status Esperado |
|---|-----------|--------|----------|-------------|-----------------|
| 1 | Email duplicado en registro | POST | `/api/auth/register` | `{ "name": "Alice2", "email": "alice@test.com", "password": "pass123" }` | 400 Bad Request |
| 2 | Contraseña incorrecta en login | POST | `/api/auth/login` | `{ "email": "alice@test.com", "password": "wrong" }` | 400 Bad Request |
| 3 | Acceso sin JWT token | GET | `/api/users/me` | Sin header Authorization | 401 Unauthorized |
| 4 | Acceso con JWT inválido | GET | `/api/users/me` | `Authorization: Bearer invalido` | 401 Unauthorized |
| 5 | Grupo no existente | GET | `/api/groups/00000000-0000-0000-0000-000000000000` | 🔒 | 404 Not Found |
| 6 | Gasto no existente | GET | `/api/expenses/00000000-0000-0000-0000-000000000000` | 🔒 | 404 Not Found |
| 7 | Crear gasto con split custom que no suma | POST | `/api/expenses` | `{ "splitType": "custom", "amount": 100, "splits": [{"userId": "<a>", "amount": 50}, {"userId": "<b>", "amount": 30}] }` | 400 Bad Request |
| 8 | Crear gasto con split custom duplicado | POST | `/api/expenses` | `{ "splitType": "custom", "amount": 100, "splits": [{"userId": "<a>", "amount": 60}, {"userId": "<a>", "amount": 40}] }` | 400 Bad Request |
| 9 | Delete grupo como no-Admin | DELETE | `/api/groups/{groupId}` | 🔒 Bob (Member) | 403 Forbidden |
| 10 | Quitar último Admin del grupo | DELETE | `/api/groups/{groupId}/users/<aliceId>` | 🔒 Alice (último Admin) | 400 Bad Request |
| 11 | Pago a sí mismo | POST | `/api/groups/{groupId}/payments` | `{ "fromUserId": "<aliceId>", "toUserId": "<aliceId>", "amount": 10 }` | 400 Bad Request |
| 12 | Pago con monto negativo | POST | `/api/groups/{groupId}/payments` | `{ "fromUserId": "<bobId>", "toUserId": "<aliceId>", "amount": -5 }` | 400 Bad Request |
| 13 | Pago con monto cero | POST | `/api/groups/{groupId}/payments` | `{ "fromUserId": "<bobId>", "toUserId": "<aliceId>", "amount": 0 }` | 400 Bad Request |
| 14 | Agregar usuario que ya está en el grupo | POST | `/api/groups/{groupId}/users` | `{ "userId": "<bobId>", "role": "Member" }` | 400 Bad Request |
| 15 | Delete gasto por no-creator | DELETE | `/api/expenses/<id>` | 🔒 Bob (no creó el gasto) | 403 Forbidden |

---

## Checklist de Edge Cases

### Autenticación
- [ ] Email duplicado en registro → 400
- [ ] Contraseña incorrecta en login → 400
- [ ] Access token expirado → 401
- [ ] Refresh token inválido → 400
- [ ] Refresh token revocado (después de logout) → 400
- [ ] Refresh token expirado (7 días) → 400
- [ ] Nuevo access token después de refresh es válido
- [ ] Anterior refresh token queda revocado después de hacer refresh

### Validación de Gastos (Splits)
- [ ] Split igual con 3 miembros y monto no divisible ($10 entre 3 = 3.33, 3.33, 3.34) → distribución de centavos correcta
- [ ] Split custom con IDs duplicados → 400
- [ ] Split custom donde suma de splits != amount → 400
- [ ] Split custom con monto 0 en un split → 400
- [ ] Split custom con monto negativo en un split → 400
- [ ] Crear gasto sin miembros suficientes para split igual

### Grupos
- [ ] Quitar último Admin → 400
- [ ] Agregar usuario que ya es miembro → 400
- [ ] Acceder a grupo soft-deleted → 404
- [ ] Crear grupo con nombre vacío → 400
- [ ] Crear grupo con nombre > 100 caracteres → 400

### Gastos
- [ ] Soft delete: gasto eliminado no aparece en listado → OK (no incluido)
- [ ] Soft delete: gasto eliminado no se puede obtener por ID → 404
- [ ] Delete gasto por non-creator → 403
- [ ] Update gasto por non-creator → 403
- [ ] Crear gasto con descripción vacía → 400
- [ ] Crear gasto con monto ≤ 0 → 400
- [ ] Crear gasto con monto con más de 2 decimales → 400

### Pagos
- [ ] Pago a sí mismo → 400
- [ ] Pago con monto negativo → 400
- [ ] Pago con monto cero → 400
- [ ] Pago entre usuarios no miembros del grupo → 403/400
- [ ] Pago con note > 255 caracteres → 400

### Paginación
- [ ] PageSize > 100 se limita a 100
- [ ] PageSize = 0 → usa default (10)
- [ ] Page = 0 → usa default (1)
- [ ] Page negativo → usa default (1)
- [ ] totalCount y totalPages son correctos
- [ ] Response incluye items vacíos para páginas fuera de rango

### Perfil de Usuario
- [ ] Obtener perfil propio → 200
- [ ] Actualizar nombre → 204
- [ ] Cambiar contraseña (correcta) → 204
- [ ] Cambiar contraseña (incorrecta) → 400
- [ ] Cambiar contraseña (nueva ≠ confirmación) → 400
- [ ] Cambiar contraseña (nueva < 6 chars) → 400

### Balance y Deudas
- [ ] Balance correcto después de múltiples gastos y pagos
- [ ] Simplificación de deudas con múltiples acreedores y deudores
- [ ] Balance $0 cuando todo está saldado → `simplifiedDebts: []`
- [ ] Deuda < $0.01 se ignora en simplificación

---

## cURL Quick Reference

```bash
BASE_URL="http://localhost:5050"

# ── Auth ──
curl -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"name":"Alice","email":"alice@test.com","password":"password123"}'

curl -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@test.com","password":"password123"}'

# Guardar token
TOKEN="Bearer <accessToken>"

# ── Crear Grupo ──
curl -X POST "$BASE_URL/api/groups" \
  -H "Authorization: $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Casa Compartida"}'

# ── Verificar Swagger con JWT ──
# 1. Abrir http://localhost:5050/swagger
# 2. Click "Authorize" (esquina superior derecha)
# 3. Ingresar: Bearer <accessToken>
# 4. Click "Authorize"
# 5. Todos los requests posteriores incluirán el token automáticamente
```
