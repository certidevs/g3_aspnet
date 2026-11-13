# Outline de diapositivas y notas del orador

Estructura alineada con [diapositivas-presentacion-adecco.marp.md](diapositivas-presentacion-adecco.marp.md) y el [guion de 45 minutos](guion-presentacion-adecco.md). **Bullets** = lo que ve la sala; **Notas** = lo que dices.

---

## Slide 1 — TALLER .NET

**Tiempo:** 1 min

**Bullets:**

- Gestión de taller mecánico en la web
- Clientes, reparaciones y almacén
- Proyecto formativo Grupo 3 — .NET

**Notas:** Saludo. Nombres del equipo. Objetivo de la sesión: conocer el producto y las competencias, no solo la pantalla.

---

## Slide 2 — El reto en el taller

**Tiempo:** 2 min

**Bullets:**

- Información repartida entre recepción, mecánico y almacén
- Poca visibilidad para el cliente
- Errores de stock y falta de trazabilidad

**Notas:** Ejemplo cotidiano: llamada del cliente pidiendo estado. Conectar con digitalización pyme sin mencionar marcas comerciales.

---

## Slide 3 — Nuestra respuesta

**Tiempo:** 2 min

**Bullets:**

- Un solo sistema web
- Roles: administrador, mecánico, cliente
- Reparaciones con coste y piezas trazables

**Notas:** Enlace mental al dossier. No prometer ERP completo.

---

## Slide 4 — Quién está detrás

**Tiempo:** 2 min

**Bullets:**

- Grupo 3 — equipo de desarrollo
- Formación práctica en producto real
- Repositorio abierto en GitHub

**Notas:** Mencionar mentores del README si aplica. Refuerzo empleabilidad: proyecto demostrable en entrevista.

---

## Slide 5 — Competencias que trabajamos

**Tiempo:** 1 min

**Bullets:**

- .NET 10 y ASP.NET Core MVC
- Datos con Entity Framework Core
- Identity, pruebas, Docker y CI

**Notas:** Tabla ampliada en [presentacion-adecco.md](presentacion-adecco.md#competencias-y-empleabilidad).

---

## Slide 6 — Arquitectura en una vista

**Tiempo:** 2 min

**Bullets:**

- Interfaz web (Bootstrap + Razor)
- Lógica de negocio en el servidor
- Base de datos relacional (SQLite en la demo)

**Notas:** Sin diagrama de carpetas. Tres cajas bastan.

---

## Slide 7 — Actores del sistema

**Tiempo:** 2 min

**Bullets:**

- Admin — usuarios y operación completa
- Mechanic — taller día a día
- Customer — solo sus datos
- User — registro hasta asignación de rol

**Notas:** Anticipar demo cambiando rol de `User` a `Customer`.

---

## Slide 8 — Modelo de datos (simplificado)

**Tiempo:** 2 min

**Bullets:**

- Usuario → vehículos → reparaciones
- Reparaciones consumen recambios (stock)
- Imagen o diagrama ER

**Notas:** Usar `docs/img/modelo_entidad_relacion.png` si el proyector falla con Mermaid.

---

## Slide 9 — Demo: empezamos

**Tiempo:** 1 min (transición)

**Bullets:**

- Sesión en vivo en localhost
- Cuentas de desarrollo (no producción)
- ~20 minutos de recorrido

**Notas:** Cambiar al navegador. Tema claro.

---

## Slide 10 — Demo: el cliente informado

**Tiempo:** (en vivo, parte B)

**Bullets:**

- Solo sus vehículos
- Detalle con coste y piezas
- Confianza y transparencia

**Notas:** Mientras se navega, narrar beneficios; no leer la slide.

---

## Slide 11 — Demo: el mecánico y el almacén

**Tiempo:** (en vivo, parte C)

**Bullets:**

- Reparación nueva
- Pieza consumida → stock actualizado
- Aviso si no hay stock

**Notas:** Momento más “técnico-visual” de la sesión.

---

## Slide 12 — Demo: precio histórico

**Tiempo:** (en vivo, parte D)

**Bullets:**

- El catálogo puede subir de precio
- Las reparaciones pasadas conservan el importe de entonces

**Notas:** Importante para talleres y facturación coherente.

---

## Slide 13 — (Pausa / preguntas breves)

**Tiempo:** 0–2 min

**Bullets:**

- ¿Alguna duda del recorrido?

**Notas:** Si van retrasados, omitir y seguir.

---

## Slide 14 — Integridad de los datos

**Tiempo:** 2 min

**Bullets:**

- No se borra un vehículo con historial
- No se borra un recambio ya usado
- Historial protegido

**Notas:** Demo rápida demo.md §4 si hay tiempo.

---

## Slide 15 — Cómo se calcula el coste

**Tiempo:** 1 min

**Bullets:**

- Horas × tarifa + piezas
- Se calcula al consultar, no “a ojo”

**Notas:** Opcional: fórmula en guía funcional.

---

## Slide 16 — Stock automático

**Tiempo:** 1 min

**Bullets:**

- Alta, edición y baja de piezas mueven el almacén
- Cantidades con límites razonables

**Notas:** Refuerza pilar “almacén conectado”.

---

## Slide 17 — Calidad y método

**Tiempo:** 1 min

**Bullets:**

- Tests automatizados
- Integración continua en GitHub
- Pull requests y documentación

**Notas:** Badge CI del README.

---

## Slide 18 — Madurez hoy

**Tiempo:** 2 min

**Bullets:**

- Prototipo funcional avanzado
- Entorno de formación y demo
- Base de datos recreada en cada arranque (por ahora)

**Notas:** Tono neutral: siguiente paso claro, no excusa.

---

## Slide 19 — Roadmap hacia un piloto

**Tiempo:** 2 min

**Bullets:**

- Persistencia y migraciones
- Seguridad por rol en taller
- Alertas de stock mínimo
- Piloto con empresa colaboradora

**Notas:** Seis hitos en dossier; no leer todos si falta tiempo.

---

## Slide 20 — Colaboración con empresas

**Tiempo:** 1 min

**Bullets:**

- Usuarios de prueba (recepción, mecánico, cliente)
- Feedback en semanas, sin ERP obligatorio
- RGPD y hosting en fase piloto

**Notas:** Puerta abierta a Fundación Adecco como facilitador.

---

## Slide 21 — Resumen

**Tiempo:** 1 min

**Bullets:**

- Digitalización concreta del taller
- Equipo con stack actual de mercado
- Listos para siguiente fase con socio

**Notas:** Una frase memorable por persona del equipo (opcional).

---

## Slide 22 — Gracias / contacto

**Tiempo:** 1 min

**Bullets:**

- Repositorio: certidevs/g3_aspnet
- Documentación y demo en el wiki
- Preguntas

**Notas:** QR o URL corta si lo tenéis preparado.
