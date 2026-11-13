# Guion de presentación (45 minutos)

Sesión orientada a **Fundación Adecco**, empresas y consultores. Material complementario: [dossier](presentacion-adecco.md), [diapositivas (outline)](diapositivas-presentacion-adecco.md), [demo técnica](demo.md).

## Checklist antes de la reunión

- [ ] `dotnet run --project G3AspNet.csproj --launch-profile http` (o Docker) y URL <http://localhost:5107> accesible.
- [ ] Datos de ejemplo cargados (`SeedData:Enabled` en `true` por defecto).
- [ ] Navegador en **tema claro** (mejor en proyector).
- [ ] Diapositivas exportadas desde [Marp](diapositivas-presentacion-adecco.marp.md) o pestaña del outline abierta.
- [ ] Credenciales: tabla en [demo.md](demo.md) (no compartir como material impreso en producción).
- [ ] Plan B: capturas o [diagrama ER](diagrama-er.md) / [PNG](img/modelo_entidad_relacion.png) si falla la red local.
- [ ] Segundo portátil o vídeo grabado de la demo (opcional).

## Reparto de tiempo

| Minutos | Bloque | Responsable sugerido |
|:---:|:---|:---|
| 0–8 | Contexto, equipo y empleabilidad | Cualquier miembro |
| 8–12 | Arquitectura ligera (sin código) | Perfil técnico |
| 12–32 | Demo en vivo | Quien domine el flujo mecánico + cliente |
| 32–38 | Reglas de negocio e integridad | Perfil técnico |
| 38–43 | Madurez y roadmap | Coordinador |
| 43–45 | Cierre y preguntas | Todos |

Margen: si hay preguntas durante la demo, **acortar** el bloque 8–12 o la sección 6 del demo.

---

### 0–8 min — Contexto y equipo

**Objetivo:** que la sala entienda el problema y por qué importa para empleabilidad.

1. Presentar **TALLER .NET** en una frase (gestión de taller + área cliente).
2. Sector: pyme automoción, trazabilidad y stock.
3. Proyecto **Grupo 3**, formación .NET; enlace al repo si la audiencia es técnica.
4. Mencionar [competencias del dossier](presentacion-adecco.md#competencias-y-empleabilidad): MVC, EF, Identity, tests, CI, Git.
5. No entrar aún en limitaciones de producción (llegan al minuto 38).

**Diapositivas:** 1–5 del outline.

---

### 8–12 min — Arquitectura ligera

**Objetivo:** confianza sin pantallazo de código.

1. Stack: .NET 10, MVC, EF Core, SQLite, Identity, Bootstrap.
2. Tres capas mentales: **navegador → aplicación → base de datos**.
3. Cuatro roles: Admin, Mechanic, Customer, User.
4. Mostrar diagrama ER simplificado (slide o [PNG](img/modelo_entidad_relacion.png)): usuario, vehículo, reparación, pieza, recambio.

**Diapositivas:** 6–8.

---

### 12–32 min — Demo en vivo (~20 min)

Seguir [demo.md](demo.md). Orden optimizado para audiencia mixta:

#### A. Administración (≈4 min) — demo.md §1

1. Login `admin` / contraseña en [demo.md](demo.md).
2. **Administración de usuarios**: buscar `user`, cambiar rol a `Customer`.
3. Intentar borrar la propia cuenta → mensaje de protección.

#### B. Vista cliente (≈5 min) — demo.md §2

1. Logout; login `customer`.
2. **Vehículos**: solo `1234ABC`, `5678DEF`, `9012GHI`.
3. Detalle de una **reparación**: mecánico, estado, piezas, **coste total**.
4. (Opcional rápido) `customer2` con otras matrículas.

#### C. Trabajo del mecánico (≈8 min) — demo.md §3

1. Login `mechanic`.
2. Crear vehículo y asignar cliente (o usar uno existente si falta tiempo).
3. Nueva **reparación** con horas y tarifa.
4. Anotar stock de un recambio; crear línea en **Piezas usadas**.
5. Comprobar **stock reducido**; intentar cantidad superior al stock → aviso.
6. (Si hay tiempo) eliminar línea y ver stock repuesto.

#### D. Precio histórico (≈3 min) — demo.md §5

1. Reparación de `1234ABC` con aceite a 36,90 € en línea.
2. Catálogo del aceite a 38,90 €; cambiar precio en recambio.
3. Volver al detalle: **línea sin cambio**.

**No hacer en vivo** (pasar al bloque 32–38): protección al borrar vehículo con reparaciones (demo.md §4).

**Plan B:** narrar los pasos con el [dossier](presentacion-adecco.md#demostración-recomendada-en-reunión) y mostrar capturas.

---

### 32–38 min — Reglas de negocio

**Objetivo:** demostrar que no es un CRUD genérico.

1. **Protección del historial** — demo.md §4: no borrar vehículo con reparaciones ni recambio usado.
2. Fórmula de coste: mano de obra + piezas (sin total almacenado; se calcula).
3. Stock automático al añadir, editar o quitar piezas.
4. Matrícula y referencia de recambio únicas.

Enlace a detalle: [guía funcional — reglas](guia-funcional.md#reglas-de-negocio).

**Diapositivas:** 14–17.

---

### 38–43 min — Madurez y roadmap

**Objetivo:** transparencia sin restar mérito formativo.

1. Estado: prototipo avanzado, **en desarrollo**; CI y tests.
2. Hoy: base de datos **recreada al arranque** — adecuado para formación y demo, no para producción directa.
3. Roadmap de 6 hitos del [dossier](presentacion-adecco.md#roadmap-hacia-un-piloto).
4. Propuesta de colaboración: piloto con taller, usuarios de prueba, 4–6 semanas de feedback.

**Diapositivas:** 18–20.

---

### 43–45 min — Cierre

1. Recapitular: problema, solución, competencias, siguiente paso.
2. Invitar preguntas (FAQ en [dossier](presentacion-adecco.md#preguntas-frecuentes-consultores-y-empresas)).
3. Dejar contacto del equipo ([README](../README.md#equipo)) y enlace al repositorio.

**Diapositiva:** 21–22.

---

## Roles durante la sesión

| Rol | Tarea |
|---|---|
| Orador principal | Slides 1–8 y 38–45 |
| Operador demo | Navegación en la app 12–32 |
| Apoyo | Vigilar tiempo; leer FAQ si preguntan producción o RGPD |

## Preguntas difíciles — respuestas cortas

| Pregunta | Respuesta orientativa |
|---|---|
| ¿Está listo para venderse? | Prototipo para demostrar capacidad; piloto tras persistencia y seguridad por rol. |
| ¿Un cliente puede modificar reparaciones? | Diseño por roles; refuerzo de permisos en roadmap. |
| ¿Por qué SQLite? | Simplicidad en formación; piloto puede cambiar de motor. |
| ¿Cuántas personas en el equipo? | Grupo 3 — cuatro integrantes (+ mentores listados en README). |
