# GitHub Kanban Setup — Artemis Banking

## 1. Crear el repositorio
```bash
git init ArtemisBank
cd ArtemisBank
git remote add origin https://github.com/[usuario]/artemis-banking.git
```

## 2. Estructura de ramas
```
main       ← producción (protegida)
develop    ← integración continua
feature/*  ← cada funcionalidad
hotfix/*   ← bugs urgentes
```

## 3. Labels a crear en GitHub
| Label     | Color   | Uso |
|-----------|---------|-----|
| setup     | #6b7280 | Configuración inicial |
| backend   | #7c3aed | Servicios y lógica |
| frontend  | #10b981 | Vistas y UI |
| api       | #06b6d4 | Endpoints REST |
| auth      | #ef4444 | Autenticación |
| payments  | #f59e0b | Transacciones |
| infra     | #8b5cf6 | Infraestructura |
| bugfix    | #dc2626 | Bugs |
| testing   | #14b8a6 | QA |

## 4. Milestones
- **M1: Fundamentos** — Días 1-3
- **M2: Admin Panel** — Días 4-6  
- **M3: Cliente** — Día 7
- **M4: Cajero** — Día 8
- **M5: API + QA** — Días 9-10

## 5. Issues a crear (uno por tarjeta Kanban)
### Backlog
1. Configurar Hangfire para cuotas vencidas
2. API: Gestión de Comercios
3. API: Hermes Pay
4. Admin Dashboard indicadores
5. Seeding datos iniciales

### To Do (Días 1-3)
6. Setup solución Onion 5 proyectos
7. ASP.NET Identity + Roles + DbContext
8. Entidades Domain + migraciones EF
9. Generic Repository + Servicio genérico
10. Login + Reseteo contraseña (WebApp)
11. Seguridad [Authorize] + AccesoDenegado
12. Servicio Email (MailKit)
13. AutoMapper Profiles

### In Progress (Días 4-7)
14. Admin: CRUD Usuarios paginación
15. Admin: Asignación Préstamos + alto riesgo
16. Cálculo cuota francesa + tabla amortización
17. Admin: Tarjetas de Crédito
18. Admin: Cuentas de Ahorro
19. Cliente: Home + listado productos
20. Cliente: Beneficiarios CRUD
21. Cliente: Transacciones (Express, TC, Préstamo, Beneficiarios)
22. Cliente: Avance de efectivo
23. Cliente: Transferencia entre cuentas

### Review (Días 8-9)
24. Cajero: Home + indicadores
25. Cajero: Depósito + Retiro
26. Cajero: Pago TC + Pago Préstamo + Terceros
27. Web API: Account endpoints (JWT)
28. Web API: Usuarios, Préstamos, TC, Cuentas
29. Swagger configurado

### Done (Día 10)
30. Testing E2E todos los flujos
31. Bug fix + validaciones
32. Deploy + README + tag v1.0.0
