# G3 · ASP.NET Core: Gestión de talleres mecánicos administración de clientes, vehículos, reparaciones, mecánicos y órdenes de trabajo.

Repositorio del **Grupo 3** del curso de .NET: aplicación web con **C#, ASP.NET Core MVC, Razor y Entity Framework Core**, desarrollada en equipo con Git y GitHub.

## Qué incluye el esqueleto

Punto de partida común de todos los grupos: **solo la entidad `ApplicationUser` (usuario)** y la infraestructura para que todo arranque a la primera. Las entidades del dominio, sus relaciones, servicios, controladores y vistas las construye el equipo.

| Área | Contenido |
| --- | --- |
| Usuarios | `ApplicationUser` (ASP.NET Core Identity) con nombre visible, avatar, fecha de alta y estado activo; roles `Admin`, `User`, `Customer` y `Mechanic`. |
| Cuenta | Registro, login por usuario o email, logout y página de acceso denegado. |
| Perfil | Consulta y edición del perfil, avatar y cambio de contraseña. |
| Administración | Panel `/Users` (solo `Admin`): alta, edición, activación y borrado de usuarios. |
| Datos | Entity Framework Core + SQLite (`App_Data/g3.db`), base recreada y datos demo al arrancar. |
| Imágenes | `ImageStorage` para guardar imágenes subidas; hoy solo avatares (añadid vuestras carpetas en `ImageFolder`). |
| Interfaz | Razor + Bootstrap 5 en local, con tema claro/oscuro. |
| Calidad y despliegue | Proyecto de tests xUnit, GitHub Actions (compilar y testear) y `Dockerfile`. |

## Requisitos

- SDK de .NET 10. `global.json` acepta cualquier SDK de la versión 10.0.
- Visual Studio Code y C# Dev Kit. Es el IDE estándar del curso en Windows, macOS y
  Linux.
- Git. Docker es opcional y solo se usa al explicar despliegue.

No instales ASP.NET, C#, SQLite ni Entity Framework por separado: los aporta el SDK o
se restauran como dependencias del proyecto.

## Instalación inicial

Instala .NET 10 SDK, Visual Studio Code y C# Dev Kit una sola vez en el ordenador.

### Windows (PowerShell)

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact
winget install --id Microsoft.VisualStudioCode --exact
winget install --id Git.Git --exact
```

Cierra y abre una terminal nueva; después instala la extensión:

```powershell
code --install-extension ms-dotnettools.csdevkit
```

### macOS (Terminal, con Homebrew)

```bash
brew install dotnet
brew install --cask visual-studio-code
brew install git
code --install-extension ms-dotnettools.csdevkit
```

Si no utilizas Homebrew, instala el SDK de .NET 10 con el instalador oficial de
[macOS](https://dotnet.microsoft.com/download/dotnet/10.0) y añade C# Dev Kit desde
el panel Extensions de VS Code.

### Ubuntu 26.04 (Terminal)

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 git
sudo snap install code --classic
code --install-extension ms-dotnettools.csdevkit
```

Para otra distribución Linux, sigue el instalador oficial de
[.NET para Linux](https://learn.microsoft.com/dotnet/core/install/linux) y, una vez
instalado VS Code, ejecuta el último comando.

Comprueba la instalación desde esta carpeta:

```bash
dotnet --version
```

Debe mostrar una versión que empiece por `10.0`.
Si el comando `code` no se reconoce, abre VS Code e instala C# Dev Kit desde
**Extensions**.

## Primer arranque

Clona el repositorio y abre **su carpeta** (no una carpeta superior) en VS Code. C# Dev Kit carga automáticamente `G3AspNet.sln`.

```bash
git clone https://github.com/certidevs/g3_aspnet.git
cd g3_aspnet
code .
dotnet restore G3AspNet.sln
dotnet tool restore
dotnet run --project G3AspNet.csproj --launch-profile http
```

Abre `http://localhost:5107`. El primer arranque crea `App_Data/g3.db`, crea las tablas y añade los usuarios demo:

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| `admin` | `Admin123!` | Admin |
| `user` | `User123!` | User |
| `customer`, `customer2`, `customer3` | `Customer123!` | Customer (dueños de los vehículos) |
| `mechanic`, `mechanic2`, `mechanic3` | `Mechanic123!` | Mechanic (asignados a las reparaciones) |

También carga vehículos, recambios, reparaciones y piezas usadas (con todos los estados y tipos de reparación) para poder probar todas las pantallas.

## Trabajo en equipo en GitHub

Todo el grupo tiene permiso de escritura en este repositorio y **se trabaja directamente en `main`**. Para no pisaros el trabajo:

1. **Antes de empezar a trabajar y antes de cada `git push`, haced `git pull`.** Siempre.
2. **Commits pequeños y con mensaje claro**, un commit por cambio (`feat: ...`, `fix: ...`, `docs: ...`), y subidlos pronto con `git push`.
3. **No toquéis la entidad, el controlador ni las vistas de otra persona sin avisarle** (mirad la tabla de abajo).
4. **No hagáis `git push --force`.**
5. Si al hacer `git pull` hay un conflicto, no lo resolváis a ciegas: avisad al equipo y resolvedlo juntos en VS Code.
6. Quien ya sepa usar ramas y Pull Requests puede usarlas, pero no es obligatorio.
7. Repartid el trabajo con **Issues** (una persona por issue) y, si queréis, un tablero de **Projects**.

### Entidades y quién las hace

Cada entidad tiene su carpeta de vistas, su controlador y su issue. Hoy todas las pantallas tienen **listado (`Index`) y detalle (`Details`)**; lo que falta (`Create`, `Edit`, `Save`, `Delete` y el formulario `Form`) es el trabajo de cada responsable y está marcado con `TODO (issue #N)` en el controlador y en la vista.

| Entidad | Controlador | Vistas |
| --- | --- | --- |
| `Vehicle`: vehículo de un cliente. [#4](https://github.com/certidevs/g3_aspnet/issues/4), Rafa-Gamero | `VehicleController`. [#13](https://github.com/certidevs/g3_aspnet/issues/13), rubogar | `Views/Vehicle/`. [#14](https://github.com/certidevs/g3_aspnet/issues/14), rubogar |
| `Repair`: reparación de un vehículo con su mecánico. [#5](https://github.com/certidevs/g3_aspnet/issues/5), irilopa | `RepairController`. [#15](https://github.com/certidevs/g3_aspnet/issues/15), irilopa | `Views/Repair/`. [#16](https://github.com/certidevs/g3_aspnet/issues/16), Rafa-Gamero |
| `RepairPart`: pieza usada en una reparación (tabla intermedia entre `Repair` y `SparePart`). [#7](https://github.com/certidevs/g3_aspnet/issues/7), irilopa | `RepairPartsController`. [#17](https://github.com/certidevs/g3_aspnet/issues/17), irilopa | `Views/RepairParts/`. [#18](https://github.com/certidevs/g3_aspnet/issues/18), Rafa-Gamero |
| `SparePart`: recambio del almacén. [#6](https://github.com/certidevs/g3_aspnet/issues/6), ricardohsm1996 | `SparePartsController`. [#19](https://github.com/certidevs/g3_aspnet/issues/19), ricardohsm1996 | `Views/SpareParts/`. [#20](https://github.com/certidevs/g3_aspnet/issues/20), ricardohsm1996 |

Las pantallas están en el menú **Taller** de la barra superior.

**Ejemplo completo para copiar: `SparePart`** (`SparePartsController` y `Views/SpareParts/`, con `Create`, `Edit`, `Save`, `Delete` y `Form` funcionando). En el resto el código de crear, editar y borrar ya está escrito pero **comentado**: buscad el marcador `DESCOMENTA` en controladores y vistas, quitad el comentario y ajustadlo a vuestra entidad (en `Repair` y `RepairParts` las acciones del controlador ya están vivas y solo falta descomentar los formularios de `Create`, `Edit` y `Delete`).

Relaciones y borrado (configuradas en `Data/ApplicationDbContext.cs`):

- Un cliente (`ApplicationUser`) tiene varios `Vehicle`; un vehículo tiene varias `Repair`; un mecánico (`ApplicationUser`) tiene varias `Repair` asignadas.
- Una `Repair` tiene varias líneas `RepairPart`, y cada línea apunta a un `SparePart` con su cantidad y su precio unitario en ese momento.
- Al borrar, todo es `Restrict` (la base de datos lo impide si hay datos relacionados, así que el controlador debe comprobarlo y avisar) salvo `RepairPart` -> `Repair`, que es `Cascade`: al borrar una reparación se borran sus líneas de piezas.

### Base de datos en desarrollo

Este proyecto **no usa migraciones**. Al arrancar, la aplicación borra `App_Data/g3.db`, la vuelve a crear a partir de las entidades y carga los datos demo de `Data/DbInitializer.cs` (es el equivalente a `create-drop` con H2 en Spring Boot). Por eso:

- Si cambias una entidad o una relación, **solo tienes que volver a ejecutar la aplicación**.
- Si haces `git pull` con cambios de otras personas, **solo tienes que volver a ejecutarla**.
- No hay que ejecutar `dotnet ef` ni borrar la base a mano.
- Los datos que crees a mano al probar se pierden al reiniciar. Lo que deba existir siempre se añade a `DbInitializer`.

## Comandos de trabajo diario

Todos se ejecutan desde la raíz de este repositorio.

```bash
# Restaurar dependencias NuGet y herramientas locales tras clonar o actualizar
dotnet restore G3AspNet.sln
dotnet tool restore

# Ejecutar la aplicación
dotnet run --project G3AspNet.csproj --launch-profile http

# Ejecutar con recarga al guardar archivos
dotnet watch --project G3AspNet.csproj run --launch-profile http

# Compilar y ejecutar los tests
dotnet build G3AspNet.sln
dotnet test G3AspNet.sln
```

### Entity Framework y SQLite

La base de datos se recrea al arrancar (ver *Base de datos en desarrollo*): no hay migraciones que crear ni aplicar.

### CI y Docker

[GitHub Actions](.github/workflows/build-and-test.yml) ejecuta restauración, compilación Release y tests en cada `push` a `main` (y en las Pull Requests, si alguien las usa). Si el *check* sale en rojo tras vuestro `push`, corregidlo pronto.

Docker es opcional:

```bash
docker build -t g3_aspnet:local .
docker run --rm -p 10000:10000 g3_aspnet:local
```

El contenedor queda disponible en `http://localhost:10000`.

## IDEs

En VS Code, selecciona el perfil `http` y pulsa F5 para depurar. Las tareas `G3: compilar` y `G3: ejecutar tests` están disponibles en **Terminal → Run Task**. En Windows, Visual Studio 2026 también abre `G3AspNet.sln`; consulta [la guía de IDEs](docs/IDE-SETUP.md).
