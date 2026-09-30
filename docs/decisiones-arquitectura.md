# Decisiones de arquitectura (ADR)

> Registro de las decisiones que sustentan [architecture.md](architecture.md). Formato: contexto → decisión → alternativas → consecuencias.
> Estados: **Aceptada** (vigente) · **Propuesta** (pendiente de aprobación del área de Arquitectura) · **Reemplazada**.

| ADR | Decisión | Estado |
|---|---|---|
| [001](#adr-001--microservicios-por-bounded-context-con-base-de-datos-por-servicio) | Microservicios por bounded context con base de datos por servicio | Propuesta |
| [002](#adr-002--broker-de-mensajería-rabbitmq-en-local-y-sns--sqs-en-aws-vía-masstransit) | Broker: RabbitMQ en local y SNS + SQS en AWS, vía MassTransit | Propuesta |
| [003](#adr-003--transactional-outbox-para-publicar-eventos) | Transactional Outbox para publicar eventos | Aceptada |
| [004](#adr-004--consumidor-idempotente-reintentos-y-dlq) | Consumidor idempotente + reintentos + DLQ | Aceptada |
| [005](#adr-005--postgresql-como-motor-relacional) | PostgreSQL como motor relacional | Aceptada |
| [006](#adr-006--persistencia-políglota-en-la-arquitectura-objetivo) | Persistencia políglota en la arquitectura objetivo | Propuesta |
| [007](#adr-007--prevención-de-sobreventa-con-actualización-condicional-y-sala-de-espera) | Prevención de sobreventa: actualización condicional + sala de espera | Propuesta |
| [008](#adr-008--saga-orquestada-para-la-compra) | Saga orquestada para la compra | Propuesta |
| [009](#adr-009--identidad-oidc--oauth-20-con-idp-gestionado-jwt-local-en-el-mvp) | Identidad OIDC / OAuth 2.0 con IdP gestionado; JWT local en el MVP | Aceptada |
| [010](#adr-010--ecs-fargate-como-cómputo-principal) | ECS Fargate como cómputo principal | Propuesta |
| [011](#adr-011--arquitectura-limpia--ddd--cqrs-ligero-y-licencias-de-librerías) | Arquitectura limpia + DDD + CQRS ligero; licencias de librerías | Aceptada |
| [012](#adr-012--mensajes-con-estado-y-versionado-explícito) | Mensajes con estado (*event-carried state*) y versionado explícito | Aceptada |
| [013](#adr-013--qr-firmado-para-check-in-offline) | QR firmado para check-in offline | Propuesta |
| [014](#adr-014--observabilidad-con-opentelemetry) | Observabilidad con OpenTelemetry | Aceptada |

---

## ADR-001 · Microservicios por bounded context con base de datos por servicio

**Contexto.** La plataforma combina dominios con necesidades distintas: el catálogo (lectura intensiva), el inventario (consistencia fuerte bajo picos), los pagos (integración externa y auditoría) y el check-in (baja latencia, offline). Además, debe evolucionar (promociones, marketplace).

**Decisión.** Diez microservicios alineados a *bounded contexts* (ver §4 de la arquitectura). Cada uno es **dueño exclusivo de sus datos**: nadie accede a la base de otro y la integración ocurre solo por API o por eventos.

**Alternativas.**
- *Monolito modular:* menor costo operativo inicial, pero no permite escalar el inventario por separado y todo el sistema comparte las fallas.
- *Base de datos compartida:* acopla esquemas y despliegues.

**Consecuencias.**
- ✅ Escalado y despliegue independientes; fallas aisladas.
- ⚠️ Consistencia eventual entre contextos, lo que exige outbox, idempotencia y sagas (ADR-003, 004 y 008).
- ⚠️ Mayor costo operativo, que se mitiga con plantillas de servicio, IaC y observabilidad estándar.
- 💡 Para optimizar costos, varios servicios pueden compartir un **clúster** Aurora con **bases y usuarios separados**.

---

## ADR-002 · Broker de mensajería: RabbitMQ en local y SNS + SQS en AWS, vía MassTransit

**Contexto.** El reto pide comunicación asíncrona con un broker. El MVP corre en `docker-compose` y el objetivo es AWS.

**Decisión.** Se programa contra **MassTransit 8** (abstracción de transporte). En local y en el MVP se usa **RabbitMQ**; en AWS, **SNS** (tópico por tipo de evento, *fan-out*) + **SQS** (una cola por consumidor, con DLQ por *redrive policy*).

**Alternativas.**
- *Amazon MQ for RabbitMQ:* paridad total con local, pero es un broker que hay que dimensionar y parchar. Queda como opción para el escenario híbrido.
- *Amazon EventBridge:* excelente para integraciones y reglas, pero con menor *throughput* por cuenta y más latencia. Se reserva para integraciones con terceros.
- *Kafka / MSK:* útil para *streaming* y *replay* masivo, pero sobredimensionado para el caso actual.

**Consecuencias.**
- ✅ Cambiar de transporte es configuración, no código.
- ✅ MassTransit aporta reintentos, cola de error, outbox/inbox, sagas y trazas de OpenTelemetry.
- ⚠️ La v9 de MassTransit tiene licencia comercial, por eso se fija la **v8 (Apache 2.0)**. Revisar su ventana de soporte (ADR-011).
- ⚠️ En producción con RabbitMQ se recomiendan **quorum queues**.

---

## ADR-003 · Transactional Outbox para publicar eventos

**Contexto.** Si se guarda en la base y luego se publica en el broker como dos pasos separados (*dual write*), una caída entre ambos pierde el mensaje o lo publica sin datos persistidos.

**Decisión.** El productor escribe el mensaje en una tabla **outbox** dentro de la **misma transacción** que el agregado. Un *relay* (el *delivery service* del outbox de MassTransit con EF Core) lo publica después y lo marca como entregado.

**Alternativas.** *Change Data Capture* (Debezium): más infraestructura. *Publicar después del commit y rezar*: descartado.

**Consecuencias.**
- ✅ Garantiza que ningún evento persistido se quede sin publicar.
- ⚠️ La entrega es *at-least-once*: puede haber duplicados, por eso los consumidores son idempotentes (ADR-004).
- ⚠️ Agrega una latencia de publicación de 1 s o menos (`QueryDelay`), aceptable para el caso.

---

## ADR-004 · Consumidor idempotente, reintentos y DLQ

**Contexto.** Con entrega *at-least-once* y fallas transitorias (SMTP, base de datos), el consumidor puede recibir un mensaje repetido o fallar al procesarlo.

**Decisión.**
1. **Idempotencia** por `messageId` del payload: índice único en `notifications.message_id` (inbox). Si el mensaje ya se procesó (`Sent`), se confirma (*ack*) y se ignora. El `MessageId` del sobre de MassTransit se iguala al `messageId` del contrato.
2. **Reintentos** en proceso con intervalos crecientes (1 s, 5 s, 15 s). Las excepciones no transitorias, como una validación, no se reintentan.
3. **DLQ:** al agotarse los reintentos, MassTransit mueve el mensaje a `<cola>_error` y publica `Fault<T>`. Un consumidor del *fault* marca la notificación como `Failed`. El reproceso es manual (*shovel* o *redrive*).

**Consecuencias.**
- ✅ El procesamiento es *effectively-once* en la base.
- ⚠️ El envío del correo sigue siendo *at-least-once*: si el proceso cae justo entre el envío SMTP y el `UPDATE` a `Sent`, el reintento puede duplicar el correo. Es aceptable para una notificación y queda documentado.

---

## ADR-005 · PostgreSQL como motor relacional

**Contexto.** El reto permite SQL Server o PostgreSQL (hay que elegir uno).

**Decisión.** **PostgreSQL** (17 en local, Aurora PostgreSQL en AWS).

**Razones.**
- Sin costo de licencia en ningún entorno.
- Imagen de contenedor liviana.
- Soporte de primer nivel en EF Core (Npgsql) y en el outbox de MassTransit.
- `jsonb` para guardar payloads.
- Camino directo a Aurora PostgreSQL.

**Consecuencias.** Cambiar a SQL Server implica cambiar el proveedor de EF Core, la configuración del outbox (`UseSqlServer()`) y el control de concurrencia (`rowversion` en lugar de `xmin`).

---

## ADR-006 · Persistencia políglota en la arquitectura objetivo

**Decisión.** El motor se elige según el patrón de acceso de cada servicio (tabla en §8 de la arquitectura):
- **PostgreSQL** donde hay transacciones e invariantes (eventos, inventario, órdenes, pagos, usuarios).
- **DynamoDB** para acceso por clave con alto volumen (tickets, check-in, notificaciones).
- **OpenSearch** para búsqueda.
- **Redis** para caché.
- **S3 Object Lock** para la auditoría.

**Consecuencias.**
- ✅ Cada servicio queda optimizado para su carga.
- ⚠️ Hay más tecnologías que operar: se limita a estas 5 y todas son gestionadas por AWS. Las decisiones pasan por la revisión del área de Base de Datos.

---

## ADR-007 · Prevención de sobreventa con actualización condicional y sala de espera

**Contexto.** En la apertura de una preventa, miles de usuarios compiten por el mismo stock. La sobreventa es inaceptable.

**Decisión.**
- La **invariante la garantiza la base de datos**: `UPDATE ... WHERE available >= @qty` atómico, `CHECK (available >= 0)` y el hold en la misma transacción.
- Los holds tienen **TTL** (10 min) y un *worker* los libera.
- La **sala de espera virtual** controla el ritmo de admisión.
- Las zonas muy demandadas usan **contadores particionados**.

**Alternativas.**
- *Contador en Redis como fuente de verdad:* muy rápido, pero exige conciliación y hay riesgo de pérdida ante una conmutación por error. Solo se usa para mostrar la disponibilidad.
- *Bloqueos pesimistas largos (`SELECT FOR UPDATE` durante el checkout):* baja concurrencia.
- *Escrituras condicionales en DynamoDB:* viables, pero un único contador caliente choca con el límite por partición.

**Consecuencias.**
- ✅ Correcta por construcción y fácil de probar.
- ⚠️ El *throughput* por zona está acotado por la contención de la fila, lo que se mitiga con *sharding* y el control de admisión.

---

## ADR-008 · Saga orquestada para la compra

**Contexto.** La compra abarca inventario, pago, emisión y notificación, en distintos servicios. No se usan transacciones distribuidas (2PC).

**Decisión.** order-service orquesta la **saga** con una *state machine* de MassTransit persistida en su base:
`PendingPayment → Paid → TicketsIssued → Completed`, con **compensaciones** (`ReleaseHold`, `RefundPayment`) ante un rechazo, un *timeout* o la expiración del hold.

**Alternativas.** *Coreografía pura:* menos acoplamiento, pero el flujo queda disperso y es difícil de observar y depurar en un proceso de 4 o más pasos con compensaciones.

**Consecuencias.**
- ✅ Flujo explícito, observable y testeable.
- ⚠️ order-service se vuelve crítico, por eso se escala horizontalmente con estado en la base.

---

## ADR-009 · Identidad OIDC / OAuth 2.0 con IdP gestionado; JWT local en el MVP

**Decisión (objetivo).**
- **Amazon Cognito** como IdP.
- Flujos: Authorization Code + PKCE para las SPA y la PWA; Client Credentials entre servicios; federación con el IdP corporativo si aplica.
- Roles como grupos del IdP y validación del JWT en API Gateway y en cada servicio.

**Decisión (MVP).**
- **JWT local** con issuer propio, firmado con HS256 y una clave de al menos 32 bytes tomada de una variable de entorno.
- Un endpoint `POST /auth/dev-token` emite tokens de prueba y **solo está disponible en `Development`**.
- La validación usa los mismos parámetros estándar (`iss`, `aud`, `exp`, firma), así que migrar a Cognito o Keycloak consiste en cambiar `Authority` y `Audience` y pasar a RS256/JWKS.

**Alternativas.** *Keycloak en docker-compose:* es un OIDC real en local y queda como bonus opcional, pero cuesta tiempo dentro de las 48 h del reto.

---

## ADR-010 · ECS Fargate como cómputo principal

**Decisión.** Las APIs y los *workers* .NET corren en **ECS Fargate**. **Lambda** se usa para tareas cortas o disparadas por eventos (webhooks, PDFs, tareas programadas).

**Alternativas.**
- *EKS:* más flexible, pero exige mayor madurez operativa. Se reconsidera si la organización ya opera Kubernetes (escenario híbrido).
- *Todo en Lambda:* los arranques en frío y los límites de conexiones a PostgreSQL penalizan las APIs de alta concurrencia.

**Consecuencias.** Los contenedores son portables (local = nube) y no hay que gestionar nodos. El costo por hora es algo mayor que en EC2 con una carga estable.

---

## ADR-011 · Arquitectura limpia + DDD + CQRS ligero; licencias de librerías

**Decisión.**
- Cada servicio se divide en `Domain` (agregados, *value objects*, invariantes, sin dependencias), `Application` (casos de uso, validación, puertos), `Infrastructure` (EF Core, MassTransit, Redis, MailKit) y `Api` (HTTP, autenticación, *composition root*). Las dependencias apuntan hacia el dominio.
- **CQRS ligero:** los *commands* pasan por el agregado y el repositorio; las *queries* proyectan directo a DTO (`AsNoTracking`).
- **Licencias** (revisar las condiciones vigentes antes de producción):
  - **MassTransit v8** (Apache 2.0); la v9 es comercial.
  - **MediatR:** usar la **12.x** (última Apache 2.0) o validar la licencia comercial o comunitaria de versiones posteriores. Alternativa: un *dispatcher* propio mínimo.
  - **AutoMapper:** **no se usa**; el mapeo es explícito (métodos de extensión), más legible y sin riesgo de licencia.
  - **FluentAssertions 8+** es comercial: para las pruebas se usa **Shouldly** o **AwesomeAssertions**.
  - FluentValidation, Polly, MailKit, Serilog y OpenTelemetry tienen licencias abiertas.

---

## ADR-012 · Mensajes con estado y versionado explícito

**Contexto.** El correo de notificación necesita los datos del evento (fecha, lugar, zonas).

**Decisión.** `EventCreated` lleva el **estado necesario** (*event-carried state transfer*): los campos mínimos del reto más `date`, `venue` y `zones`. Así, notification-service no llama de vuelta a event-service, lo que evita acoplamiento temporal. El contrato tiene `version` y un nombre de *exchange* versionado (`event-created.v1`).

**Consecuencias.**
- ✅ El consumidor es autónomo aunque event-service esté caído.
- ⚠️ Los mensajes pesan más, pero no deben llevar PII. Los cambios incompatibles se publican como `v2` en paralelo.

---

## ADR-013 · QR firmado para check-in offline

**Decisión.**
- El QR contiene `ticketId`, `eventId`, `zone` y `exp`, firmados con **Ed25519**. La clave privada vive en AWS KMS / Secrets Manager.
- La app de staff descarga el **manifiesto** del evento y la clave pública, así que puede validar sin red.
- Al reconectarse sincroniza los escaneos; si hay duplicados, **gana el primer escaneo** y se genera una alerta.

**Consecuencias.**
- ✅ El ingreso sigue funcionando aunque falle la conectividad del recinto.
- ⚠️ Un ticket reenviado o revocado después de descargar el manifiesto puede no reflejarse offline. Se mitiga con la resincronización periódica del manifiesto cuando hay red.

---

## ADR-014 · Observabilidad con OpenTelemetry

**Decisión.**
- Se instrumenta con **OpenTelemetry**: ASP.NET Core, HttpClient, Npgsql, MassTransit y Redis.
- Se exporta por OTLP. En local, a la consola y opcionalmente a Jaeger; en AWS, al ADOT Collector → CloudWatch / X-Ray.
- Logs con Serilog en JSON, correlacionados por `traceId` y `correlationId`.

**Consecuencias.** Queda neutral respecto del proveedor y una sola traza cubre HTTP y mensajería.
