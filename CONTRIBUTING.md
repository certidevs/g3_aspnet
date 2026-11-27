# Guía de contribución

Cómo trabajar en el repositorio: ramas, commits, pull requests y reglas de código.

## Flujo de trabajo

Una tarea, una rama. Los cambios llegan a `main` mediante pull request.
El flujo está dibujado en [`flujo-ramas-pull-request.excalidraw`](flujo-ramas-pull-request.excalidraw).

<!-- TODO: `.github/copilot-instructions.md` dice que el equipo trabaja directamente sobre `main`, y el diagrama dice que nadie escribe en `main`. Esta guía sigue el diagrama, que es lo que refleja el historial de pull requests. Confirmar y unificar. -->

```mermaid
flowchart LR
    A["0. main al día<br/>git switch main<br/>git pull"] --> B["1. Crear rama<br/>git switch -c mi-rama"]
    B --> C["2. Trabajar y commit"]
    C --> D["3. Subir la rama<br/>git push -u origin mi-rama"]
    D --> E["4. Abrir pull request<br/>mi-rama → main"]
    E --> F["5. Revisión y merge"]
    F --> G["6. Volver a main<br/>y borrar la rama"]
```

### Pasos

1. Actualiza `main`:

   ```bash
   git switch main
   git pull
   ```

2. Crea tu rama desde `main`:

   ```bash
   git switch -c nombre-de-la-rama
   ```

3. Trabaja con commits pequeños:

   ```bash
   git add .
   git commit -m "feat: descripción breve"
   ```

4. Sube la rama:

   ```bash
   git push -u origin nombre-de-la-rama
   ```

5. Abre una pull request hacia `main` en GitHub.
6. Espera la revisión y la aprobación. <!-- TODO: indicar quién revisa; el diagrama nombra a una persona concreta. -->
7. Fusiona la pull request y borra la rama en GitHub.
8. Limpia en local:

   ```bash
   git switch main
   git pull
   git branch -d nombre-de-la-rama
   ```

### Si `main` avanza mientras trabajas

Hazlo antes de abrir la pull request, para resolver los conflictos en tu rama:

```bash
git switch main
git pull
git switch nombre-de-la-rama
git merge main
```

## Mensajes de commit

El historial usa prefijos al estilo Conventional Commits, como `feat:` y `docs:`.
Escribe un mensaje breve que explique qué cambia.

## Integración continua

Cada pull request hacia `main` ejecuta el workflow [`build-and-test.yml`](.github/workflows/build-and-test.yml).
Restaura, compila en Release y lanza las pruebas. Comprueba que pasa antes de pedir revisión.

Para reproducirlo en local:

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln --configuration Release --no-restore
dotnet test G3AspNet.sln --configuration Release --no-build
```

## Reglas de código

- Los espacios de nombres siguen las carpetas, con la raíz `G3AspNet`.
- Se usan tipos de referencia anulables, `using` implícitos, espacios de nombres de archivo y sangría de cuatro espacios. Está configurado en [`.editorconfig`](.editorconfig).
- Los controladores se mantienen simples: usa view models, comprueba `ModelState.IsValid` en los formularios y delega la lógica de cuentas en `UserService`.
- Usa `RoleNames` en lugar de escribir los nombres de rol como texto.
- Obtén el identificador del usuario con `ClaimsPrincipalExtensions.GetRequiredUserId()`.
- Toda acción exige sesión por defecto; usa `[AllowAnonymous]` solo en páginas públicas.
- Restringe por rol con las políticas de `PolicyNames` y comprueba el acceso a cada registro con `IAuthorizationService` y `WorkshopOperations`.
- Aplica en los listados el mismo filtro de visibilidad que en el detalle.
- Exige `confirmClosed` en cualquier acción que modifique una reparación cerrada.
- Si cambias una entidad, avisa a quien la mantiene. No modifiques entidades, controladores ni vistas de otra persona sin comentarlo.

## Cambios en el modelo de datos

El proyecto no usa migraciones. No las generes.

1. Modifica la entidad en [`Models/`](Models).
2. Configura la relación y su `OnDelete` en [`ApplicationDbContext`](Data/ApplicationDbContext.cs).
3. Añade filas de ejemplo en [`DbInitializer`](Data/DbInitializer.cs).
4. Actualiza el [modelo de datos](docs/diagrama-er.md).

La base de datos se recrea en cada arranque.

## Documentación

Si cambias el comportamiento visible, actualiza el documento que corresponda:

| Cambio | Documento |
|---|---|
| Mensaje para empresas, guion o diapositivas | [Presentación externa](docs/presentacion-adecco.md), [guion](docs/guion-presentacion-adecco.md), [outline](docs/diapositivas-presentacion-adecco.md), [Marp](docs/diapositivas-presentacion-adecco.marp.md) |
| Permisos o reglas de negocio | [Guía funcional](docs/guia-funcional.md) |
| Entidades, campos o enums | [Modelo de datos](docs/diagrama-er.md) |
| Cuentas o datos de ejemplo | [Demostración](docs/demo.md) |
| Arquitectura, controladores o vistas | [Arquitectura](docs/arquitectura.md), [Controladores y vistas](docs/controladores-y-vistas.md) |
| Configuración, base de datos o despliegue | [Configuración](docs/configuracion.md), [Base de datos y migraciones](docs/base-de-datos-y-migraciones.md), [Despliegue](docs/despliegue.md) |
| Ejecución, pruebas o integración continua | [Primeros pasos](docs/primeros-pasos.md), [Testing y CI](docs/testing-y-ci.md) |
| Paquetes | [Dependencias](docs/DEPENDENCIAS.md) |
| Módulo de reparaciones | [Documentación técnica](docs/documentacion-tecnica-reparaciones.md) |
| Configuración, ejecución o estructura | [README](README.md) |

## Traducción al inglés

La documentación se escribe en español. [`scripts/translate-docs.py`](scripts/translate-docs.py) la traduce al inglés con la API de DeepL y deja el resultado en `docs/en/`.

- Se traducen `README.md`, `CONTRIBUTING.md` y los Markdown de `docs/` versionados en git. Los archivos sin seguimiento no se envían a DeepL.
- No se traducen los bloques de código (tampoco los diagramas Mermaid), el código en línea, las URL, los comentarios HTML ni el front matter.
- Solo se envía el texto nuevo o modificado. El resto se reutiliza de `docs/en/.translation-cache.json`.
- No edites `docs/en/` a mano: se regenera por completo. Corrige el texto en español.

Para ejecutarlo en local, define la variable de entorno `DEEPL_API_KEY` sin escribir la clave en ningún archivo del repositorio:

```bash
python3 scripts/translate-docs.py --dry-run   # cuenta lo pendiente, sin clave
read -rs DEEPL_API_KEY && export DEEPL_API_KEY
python3 scripts/translate-docs.py
```

En GitHub, el workflow [`translate-docs.yml`](.github/workflows/translate-docs.yml) se ejecuta cuando llegan cambios de documentación a `main` y abre una pull request desde la rama `docs/traduccion-en`. Necesita:

1. El secreto `DEEPL_API_KEY` en **Settings → Secrets and variables → Actions**. Sin él, el workflow avisa y no traduce.
2. La opción **Allow GitHub Actions to create and approve pull requests**, en **Settings → Actions → General**. Sin ella, la rama se sube, pero la pull request hay que abrirla a mano.

## Wiki

El [wiki del proyecto](https://github.com/certidevs/g3_aspnet/wiki) se genera a partir de estos documentos.
El workflow [`sync-wiki.yml`](.github/workflows/sync-wiki.yml) lo actualiza en cada cambio de `README.md`, `CONTRIBUTING.md` o `docs/` que llegue a `main`.

- No edites el wiki a mano: los cambios se sobrescriben. Edita el archivo del repositorio.
- Para publicar un documento nuevo, añádelo al diccionario `PAGES` de [`scripts/build-wiki.py`](scripts/build-wiki.py).
- Un documento eliminado del repositorio no se borra del wiki. Bórralo allí a mano.
