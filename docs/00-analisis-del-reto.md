# Análisis del reto

> Punto de partida de la documentación: qué pide el reto, cómo se interpreta, qué se asume y dónde queda resuelto cada requisito.
> Enunciado original: [desarrollo-acity/reto_tecnico_lider_tecnico](https://github.com/desarrollo-acity/reto_tecnico_lider_tecnico/blob/main/README.md).

## 1. El reto en una tabla

| Parte | Qué se pide | Entregable | Documento |
|---|---|---|---|
| **A. Arquitectura** | Arquitectura completa de la plataforma: microservicios + eventos con broker, comunicación sync/async, BD SQL/NoSQL por servicio, JWT + OIDC/OAuth 2.0, AWS o híbrido, resiliencia y alta concurrencia | `docs/architecture.md` | [architecture.md](architecture.md) · [decisiones-arquitectura.md](decisiones-arquitectura.md) |
| **B. Backlog y roadmap** | Backlog con las tareas principales, Scrum con sprints de 2 semanas, equipo dado, áreas de soporte, entornos DEV → QA → Staging → Prod, documentación técnica y funcional, v1 en 6 meses | Excel + resumen | [backlog-roadmap.md](backlog-roadmap.md) · [Excel](backlog/Backlog-Roadmap-Plataforma-Eventos.xlsx) · [proceso-desarrollo.md](proceso-desarrollo.md) |
| **C. MVP** | 2 APIs .NET (EventService, NotificationService) comunicadas por colas, PostgreSQL/SQL Server, Redis, pantalla React, arquitectura limpia + DDD, Docker | Código + README + scripts + Docker | [mvp/especificacion-tecnica.md](mvp/especificacion-tecnica.md) · [mvp/plan-de-ejecucion.md](mvp/plan-de-ejecucion.md) |

**Plazo:** 2 días. **Entrega:** repositorio en GitHub, GitLab o Bitbucket.

## 2. Qué se evalúa (interpretación)

El enunciado lo dice explícitamente para el MVP: *“validar desacoplamiento, mensajería, consistencia, reintentos, idempotencia y observabilidad mínima”* y *“arquitectura limpia & DDD, event-driven y frontend básico”*. Por tratarse de un rol de **Líder Técnico**, también se asume que se evalúa:
- **Criterio de diseño:** justificar las decisiones y sus *trade-offs*, no solo listar tecnologías.
- **Planificación realista:** capacidad del equipo, dependencias con las áreas de soporte, riesgos, hitos.
- **Gobierno del proceso:** entornos, calidad, seguridad, documentación y aprobaciones.
- **Calidad de la entrega:** un repositorio que se levanta con un comando, un README claro y un historial de commits legible.

## 3. Matriz de trazabilidad de requisitos

### 3.1 MVP: funcionales

| ID | Requisito del reto | Solución | Dónde |
|---|---|---|---|
| RF-01 | `POST /events` crea el evento y sus zonas en una transacción | Agregado `Event` + `SaveChanges` único | Esp. §4, §5.1 |
| RF-02 | Publicar `EventCreated` en una cola (async) | MassTransit + **outbox** EF (misma transacción) | Esp. §7 |
| RF-03 | `GET /events` con caché Redis | Cache-aside + invalidación por versión, `X-Cache` | Esp. §5.2, §6 |
| RF-04 | (Opcional) `GET /events/{id}` | Incluido | Esp. §5 |
| RF-05 | NotificationService consume `EventCreated` | Consumidor MassTransit, cola `notifications-event-created` | Esp. §8 |
| RF-06 | Guardar `eventId`, `name`, `timestamp`, `correlationId` y `payloadHash` | Tabla `notifications` | Esp. §9.2 |
| RF-07 | Enviar un correo con el detalle del evento (MailKit) | `EmailSender` + Mailpit | Esp. §8.4 |
| RF-08 | Pantalla React “Registrar Evento” con zonas editables | React + RHF + Zod | Esp. §12 |
| RF-09 | Validación mínima (obligatorios, capacidad > 0, precio ≥ 0) y manejo de carga y error | Mismas reglas en el frontend y el backend | Esp. §5.1, §12 |
| RF-10 | Consumir `POST /events` con un token JWT | Selector de rol de demo → `dev-token` | Esp. §10, §12 |

### 3.2 MVP: mensajería, persistencia y seguridad

| ID | Requisito | Solución | Dónde |
|---|---|---|---|
| MSG-01 | Mensaje mínimo `{messageId, eventId, name, occurredAt, correlationId, version}` | Contrato `EventCreated` v1 (más `date`, `venue` y `zones`) | Esp. §7.1 |
| MSG-02 | Idempotencia del consumidor por `messageId` | Índice único + verificación de estado | Esp. §8.2 · ADR-004 |
| MSG-03 | Reintentos | `UseMessageRetry` 1 s / 5 s / 15 s | Esp. §8.3 |
| MSG-04 | DLQ o estado `Failed` (bonus fuerte) | **Ambos:** cola `_error` + `Fault<T>` → `Failed` | Esp. §8.3 |
| PER-01 | Cada API persiste en su propia BD | `events_db` y `notifications_db`, un usuario por servicio | Esp. §9 |
| PER-02 | ORM (EF) | EF Core 10 + Npgsql | Esp. §9 |
| PER-03 | `db/init.sql` o migraciones | **Ambos:** `init.sql` (roles y bases) + migraciones + scripts idempotentes | Esp. §9.3 |
| SEC-01 | JWT (local o IdP) | JWT local HS256, migrable a OIDC | Esp. §10 · ADR-009 |
| SEC-02 | Roles: `Admin` → `POST`, `User` → `GET` | Políticas `CanManageEvents` / `CanReadEvents` | Esp. §10 |
| SEC-03 | Evitar IDOR | Regla de diseño (sin endpoints por usuario en el MVP) | Esp. §10 · Arq. §10.2 |
| SEC-04 | Errores seguros | `ProblemDetails` + `IExceptionHandler` | Esp. §10 |
| SEC-05 | Rate limiting | `RateLimiter` por usuario e IP | Esp. §10 |
| SEC-06 | Logs sin datos sensibles | Serilog con filtros y enmascarado | Esp. §10, §11 |

### 3.3 Infraestructura y entregables

| ID | Requisito | Solución |
|---|---|---|
| INF-01 | Compose con `api-event`, `api-notifications`, `db(s)` y `rabbitmq` | `docker-compose.yml` con esos nombres, más `redis`, `mailpit`, `web` y `jaeger` (opcional) |
| INF-02 | Dockerfile por API | *Multi-stage*, usuario no root |
| ENT-A | Diagrama de arquitectura con componentes, microservicios, flujos, seguridad y sustentación | [architecture.md](architecture.md) (Mermaid, se ve directamente en GitHub y GitLab) |
| ENT-B | Backlog y roadmap en Excel | [Excel](backlog/Backlog-Roadmap-Plataforma-Eventos.xlsx) |
| ENT-C | README, scripts de BD, Dockerfiles y compose | Raíz del repositorio + `db/` |

## 4. Supuestos

| # | Supuesto | Impacto si cambia |
|---|---|---|
| S1 | Se elige **PostgreSQL** (el reto pide elegir uno entre SQL Server y PostgreSQL) | Cambio de proveedor EF y del outbox (ADR-005) |
| S2 | Se elige **RabbitMQ** para local; SNS/SQS en la arquitectura AWS | Solo configuración de MassTransit |
| S3 | Un evento se crea en estado `Draft`; publicar es una acción aparte (opcional en el MVP) | El usuario con rol `User` solo ve eventos publicados |
| S4 | Moneda única, configurada | Se modela `Money` en la arquitectura objetivo |
| S5 | El correo se envía a una lista de destinatarios configurada (sin PII en el mensaje) | Si se requiere notificar al creador, se consulta un servicio de usuarios |
| S6 | Token fijo o emitido por `dev-token` para la demo (el reto lo permite) | Integrar Keycloak o Cognito con OIDC |
| S7 | Volúmenes de preventa y SLO de la arquitectura objetivo son supuestos de diseño | Se validan con negocio y pruebas de carga |
| S8 | Inicio del proyecto: **lunes 5 de octubre de 2026** (ajustable en el Excel) | Las fechas del roadmap se recalculan solas |
| S9 | La **versión 1.0** incluye catálogo, búsqueda, compra, pagos, tickets, check-in y notificaciones; el marketplace y las promociones avanzadas quedan para después | Replanificación del roadmap |

## 5. Riesgos del reto (48 h) y mitigación

| Riesgo | Mitigación |
|---|---|
| El tiempo no alcanza para todo | Plan por bloques con hito de punta a punta al final del día 1 y una línea de corte explícita ([plan-de-ejecucion.md](mvp/plan-de-ejecucion.md)) |
| El entorno del evaluador no levanta | Compose con *healthchecks* y `depends_on`, migraciones automáticas en local y prueba desde un clon limpio |
| Mensajería “invisible” en la demo | Guion de demo con RabbitMQ UI, Mailpit, cabecera `X-Cache` y logs correlacionados |
| Licencias de librerías (MassTransit, MediatR, FluentAssertions) | Versiones con licencia abierta fijadas (ADR-011) |
| Documentación desalineada con el código | Revisión de `docs/` en el bloque de cierre y DoD del MVP |

## 6. Mapa de la documentación

```text
README.md                          ← portada: qué es, cómo se ejecuta, índice
docs/
├── 00-analisis-del-reto.md        ← este documento
├── architecture.md                ← ENTREGABLE A
├── decisiones-arquitectura.md     ← ADRs que sustentan la arquitectura
├── backlog-roadmap.md             ← ENTREGABLE B (resumen)
├── backlog/
│   └── Backlog-Roadmap-Plataforma-Eventos.xlsx   ← ENTREGABLE B (Excel)
├── proceso-desarrollo.md          ← Scrum, entornos, CI/CD, áreas de soporte, documentos
└── mvp/
    ├── especificacion-tecnica.md  ← diseño para construir el ENTREGABLE C
    └── plan-de-ejecucion.md       ← orden de construcción en 2 días
```
