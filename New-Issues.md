PROMT
Ahora dame los commits y stanges para subir los cambios de los issues que completamos Ayudame con estos 3 

PROMT
Ahora dame los commits y stanges para subir los cambios de los issues que completamos Ayudame con estos 3 

37 - Pago TC por cajero (validar cuenta + TC + fondos, no pagar de más)
#46
Open
Alb3rtsonTL
/
P3-Final-ArtemisBanking
Private
Open
37 - Pago TC por cajero (validar cuenta + TC + fondos, no pagar de más)
#46
Assignees
Alb3rtsonTL
20222009itla
Labels
cashier
Módulo de cajero
payments
Transacciones
Milestone
M4: Cajero - Día 8
Description
@Alb3rtsonTL
Alb3rtsonTL
opened last week
Descripción
Implementar el pago a tarjeta de crédito realizado por el cajero en nombre de un cliente.

Formulario
Número de cuenta origen (texto, 9 dígitos)
Monto a pagar (numérico)
Número de tarjeta de crédito (texto, 16 dígitos)
Todos los campos obligatorios
Validaciones
La cuenta origen debe existir y estar activa
La tarjeta de crédito debe existir y estar activa
La cuenta origen debe tener fondos suficientes para cubrir el monto
Pantalla de confirmación
Mostrar nombre y apellido del titular de la tarjeta
Confirmar o Cancelar
Al confirmar
Si monto ingresado > deuda de la tarjeta → debitar solo el monto de la deuda
(no se paga de más)
Reducir deuda de la tarjeta en el monto real pagado
Debitar de cuenta origen el monto real pagado
Registrar transacción en historial de cuenta ORIGEN:
Tipo: DÉBITO, Origen: número cuenta origen, Beneficiario: 16 dígitos de la tarjeta
Enviar correo al cliente:
Asunto: "Pago realizado a la tarjeta [últimos 4 dígitos]"
Cuerpo: monto pagado, últimos 4 dígitos cuenta origen, fecha y hora
Redirigir al Home del cajero
Criterios de aceptación

No se puede pagar más de la deuda actual de la tarjeta

Correo enviado correctamente

Si cancela → sin operación, redirige al Home del cajero

Vista protegida con [Authorize(Roles = "Cashier")]
Rama
feature/cashier-module

Activity



38 - Pago Préstamo por cajero (secuencial, excedente regresa, email)
#47
Open
Alb3rtsonTL
/
P3-Final-ArtemisBanking
Private
Open
38 - Pago Préstamo por cajero (secuencial, excedente regresa, email)
#47
Assignees
Alb3rtsonTL
20222009itla
Labels
cashier
Módulo de cajero
payments
Transacciones
Milestone
M4: Cajero - Día 8
Description
@Alb3rtsonTL
Alb3rtsonTL
opened last week · edited by Alb3rtsonTL
Descripción
Implementar el pago a préstamo realizado por el cajero.
Reutiliza la lógica secuencial de pago de cuotas (issue #30).

Formulario
Número de cuenta origen (texto, 9 dígitos)
Monto a pagar (numérico)
Número del préstamo (texto, 9 dígitos)
Todos los campos obligatorios
Validaciones
La cuenta origen debe existir y estar activa
El préstamo debe existir y estar en estado Activo
La cuenta origen debe tener fondos suficientes
Pantalla de confirmación
Mostrar nombre y apellido del titular del préstamo
Confirmar o Cancelar
Al confirmar
Aplicar pago secuencialmente cuota a cuota (misma lógica que issue Feat: background jobs – Azure Functions daily overdue quotas marker #30)
Si queda excedente → regresar a la cuenta de origen
Debitar cuenta origen el monto efectivamente pagado
Registrar en historial de cuenta origen:
Tipo: DÉBITO, Origen: número cuenta origen, Beneficiario: ID del préstamo (9 dígitos)
Enviar correo al cliente:
Asunto: "Pago realizado al préstamo [ID de 9 dígitos]"
Cuerpo: monto pagado, últimos 4 dígitos de la cuenta, fecha y hora
Si todas las cuotas quedan pagadas → marcar préstamo como Completado
Redirigir al Home del cajero
Criterios de aceptación

Reutiliza el LoanPaymentService de Application layer (no duplicar lógica)

Excedente retorna a la cuenta de origen

Correo enviado correctamente

Vista protegida con [Authorize(Roles = "Cashier")]
Dependencias
Issue Feat: background jobs – Azure Functions daily overdue quotas marker #30 (lógica de pago secuencial en Application layer)
Rama
feature/cashier-module



39 - Transacciones a cuentas de terceros (registro cruzado, 2 correos)
#48
Open
Alb3rtsonTL
/
P3-Final-ArtemisBanking
Private
Open
39 - Transacciones a cuentas de terceros (registro cruzado, 2 correos)
#48
Assignees
Alb3rtsonTL
20222009itla
Labels
cashier
Módulo de cajero
payments
Transacciones
Milestone
M4: Cajero - Día 8
Description
@Alb3rtsonTL
Alb3rtsonTL
opened last week
Descripción
Implementar la transferencia del cajero entre dos cuentas de clientes distintos,
con registro cruzado en ambas cuentas y 2 correos de notificación.

Formulario
Número de cuenta origen (texto, 9 dígitos)
Monto de la transacción (numérico)
Número de cuenta destino (texto, 9 dígitos)
Todos los campos obligatorios
Validaciones
La cuenta origen debe existir y estar activa
La cuenta origen debe tener fondos suficientes
La cuenta destino debe existir y estar activa
Pantalla de confirmación
Mostrar nombre y apellido del titular de la cuenta destino
Confirmar o Cancelar
Al confirmar — registro cruzado
Historial cuenta ORIGEN:

Tipo: DÉBITO, Origen: número cuenta origen, Beneficiario: número cuenta destino
Historial cuenta DESTINO:

Tipo: CRÉDITO, Origen: número cuenta origen, Beneficiario: número cuenta destino
Correos
Correo 1 → titular cuenta origen:
Asunto: "Transacción realizada a la cuenta [últimos 4 dígitos destino]"
Cuerpo: monto transferido, fecha y hora

Correo 2 → titular cuenta destino:
Asunto: "Transacción enviada desde la cuenta [últimos 4 dígitos origen]"
Cuerpo: monto recibido, fecha y hora

Criterios de aceptación

Registro cruzado en historial de ambas cuentas

2 correos enviados correctamente via IEmailService

Si cancela → sin operación, redirige al Home del cajero

Vista protegida con [Authorize(Roles = "Cashier")]
Rama
feature/cashier-module



41 - API Account: confirm, get-reset-token, resetpassword
#50
Open
Alb3rtsonTL
/
P3-Final-ArtemisBanking
Private
Open
41 - API Account: confirm, get-reset-token, resetpassword
#50
Assignees
Alb3rtsonTL
Labels
api
Endpoints REST
auth
Autenticación
Milestone
M5: API + QA - Días 9-10
Description
@Alb3rtsonTL
Alb3rtsonTL
opened last week
Descripción
Implementar los 3 endpoints restantes del módulo Account del WebAPI.
Todos requieren JWT excepto ninguno de ellos (estos 3 no requieren Bearer
pero sí están en el módulo Account con acceso para ambos roles).

POST /account/confirm
Confirma y activa un usuario mediante token recibido por correo.

Body: { "token": "string" }
Respuestas:
204 No Content → usuario activado
400 Bad Request → token vacío o inválido
401 Unauthorized → Bearer token ausente o inválido
POST /account/get-reset-token
Genera token para reseteo de contraseña y lo envía al correo del usuario.

Body: { "userName": "string" }
Reglas:
Validar que el usuario existe
Inactivar el usuario
Generar reset token
Enviar el token en el cuerpo del correo (NO como enlace, solo el token en texto)
Respuestas:
204 No Content → token generado y correo enviado
400 Bad Request → usuario vacío o no existe
401 Unauthorized
POST /account/reset-password
Cambia la contraseña usando el token del correo.

Body: { "userId": "string", "token": "string", "password": "string", "confirmPassword": "string" }
Reglas:
Todos los campos obligatorios
Password y confirmPassword deben coincidir
Token debe ser válido
Una vez cambiada la contraseña → reactivar usuario
Respuestas:
204 No Content → contraseña cambiada
400 Bad Request → campos faltantes, passwords no coinciden, token inválido
401 Unauthorized
Criterios de aceptación

El correo del get-reset-token envía el token en texto plano (no como link)

El usuario queda inactivo tras get-reset-token y activo tras reset-password

Reutiliza IEmailService de la capa Shared
Rama
feature/web-api



42 - API Usuarios: GET/POST/PUT/PATCH (Admin only)
#52
Open
Alb3rtsonTL
/
P3-Final-ArtemisBanking
Private
Open
42 - API Usuarios: GET/POST/PUT/PATCH (Admin only)
#52
Assignees
Alb3rtsonTL
Labels
api
Endpoints REST
auth
Autenticación
Milestone
M5: API + QA - Días 9-10
Description
@Alb3rtsonTL
Alb3rtsonTL
opened last week
Descripción
Implementar todos los endpoints del módulo Gestión de Usuarios del WebAPI.
Todos requieren Bearer JWT + rol Admin (403 para otros roles).

Endpoints
GET /api/users
Listado paginado de usuarios (excluye rol Commerce).

Query params: page (default 1), pageSize (default 20), rol (opcional)
Respuestas: 200 OK | 401 | 403
GET /api/users/commerce
Listado paginado de usuarios con rol Commerce.

Query params: page, pageSize
Respuestas: 200 OK | 401 | 403
GET /api/users/{id}
Detalle de un usuario específico.

Respuestas: 200 OK | 404 Not Found | 401 | 403
POST /api/users
Crear usuario (Admin, Cajero o Cliente).

Reglas: unicidad usuario y correo, si es cliente → crear cuenta de ahorro principal 9 dígitos
Token de activación enviado en el cuerpo del correo (no como enlace)
Respuestas: 201 Created | 400 | 409 Conflict | 401 | 403
POST /api/users/commerce/{commerceId}
Crear usuario con rol Commerce asociado a un comercio.

Reglas: el comercio solo puede tener 1 usuario; crear cuenta de ahorro principal para el usuario
Respuestas: 201 Created | 400 | 401 | 403
PUT /api/users/{id}
Actualizar datos de usuario.

No se puede modificar el tipo/rol
Si es cliente y viene montoAdicional → sumarlo al balance de su cuenta principal
Respuestas: 204 No Content | 400 | 404 | 409 | 401 | 403
PATCH /api/users/{id}/status
Cambiar estado activo/inactivo de un usuario.

Body: { "status": true }
El admin autenticado NO puede cambiar su propio estado
Respuestas: 204 No Content | 400 | 403 | 404 | 401
Criterios de aceptación

Todos los endpoints retornan los status codes correctos

La cuenta de ahorro principal para clientes/commerce se genera con 9 dígitos únicos

El token de activación llega en texto plano en el cuerpo del correo (no como link)

Todos los endpoints tienen [Authorize(Roles = "Admin")]
Rama
feature/web-api