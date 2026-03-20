# Checklist de Evaluación

## Funcionalidades Generales
- [ ] **Login** — Form, redirección por rol, ya logueado → Home
- [ ] **Validaciones login** — Credenciales incorrectas, cuenta inactiva
- [ ] **Reseteo contraseña** — Token email, desactivar, reactivar
- [ ] **Seguridad Identity** — [Authorize] roles, AccesoDenegado, seeding

## Administrador — Home
- [ ] **Indicadores dashboard** — 8 KPIs requeridos

## Administrador — Usuarios
- [ ] **Listado**
- [ ] **Paginación**
- [ ] **Creación** — Con cuenta ahorro automática para cliente
- [ ] **Edición** — MontoAdicional para cliente
- [ ] **Activar/Desactivar** — No puede modificarse a sí mismo

## Administrador — Préstamos
- [ ] **Listado**
- [ ] **Paginación**
- [ ] **Asignación** — Wizard 2 pasos
- [ ] **Cálculo cuota** — Fórmula francesa + validación alto riesgo
- [ ] **Tabla amortización** — N cuotas, fechas correctas, email, acreditar cuenta
- [ ] **Cuotas vencidas automático** — Hangfire/Quartz diario

## Administrador — Tarjetas
- [ ] **Listado**
- [ ] **Paginación**
- [ ] **Asignación** — 16 dígitos, CVC SHA-256, expiración +3 años
- [ ] **Detalle consumos** — APROBADO/RECHAZADO, AVANCE

## Administrador — Cuentas de Ahorro
- [ ] **Listado**
- [ ] **Paginación**
- [ ] **Asignación** — 9 dígitos únicos
- [ ] **Detalle transacciones**

## Cliente — Listado Productos
- [ ] **Listado cuentas** — Principal primero, secundarias por balance
- [ ] **Listado préstamos**
- [ ] **Listado tarjetas**
- [ ] **Detalle cuenta**
- [ ] **Detalle préstamo**
- [ ] **Detalle tarjeta**

## Cliente — Beneficiarios
- [ ] **Creación**
- [ ] **Listado**
- [ ] **Eliminación**

## Cliente — Transacciones
- [ ] **Express** — 2 correos
- [ ] **Pago TC** — No pagar de más
- [ ] **Pago Préstamo** — Secuencial, excedente regresa
- [ ] **Beneficiarios** — 2 correos

## Cliente — Avance de Efectivo
- [ ] **Validación crédito disponible**
- [ ] **Procesamiento + 6.25%**
- [ ] **Email**

## Cliente — Transferencia entre cuentas
- [ ] **Validación**
- [ ] **Procesamiento**

## Cajero — Home
- [ ] **Indicadores del día**

## Cajero — Depósito
- [ ] **Validaciones**
- [ ] **Email**
- [ ] **Procesamiento**

## Cajero — Retiro
- [ ] **Validaciones**
- [ ] **Procesamiento**
- [ ] **Email**

## Cajero — Pago TC
- [ ] **Validaciones**
- [ ] **Procesamiento**
- [ ] **Email**

## Cajero — Pago Préstamo
- [ ] **Validaciones**
- [ ] **Procesamiento**
- [ ] **Email**

## Cajero — Transacciones Terceros
- [ ] **Validaciones**
- [ ] **Procesamiento**
- [ ] **Email**

---
*By:* ![**Alb3rtsonTL**](https://github.com/Alb3rtsonTL)
