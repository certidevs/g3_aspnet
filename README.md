# TALLER .NET

[![CI](https://github.com/certidevs/g3_aspnet/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/certidevs/g3_aspnet/actions/workflows/build-and-test.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](G3AspNet.csproj)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)](Program.cs)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0.11-512BD4?logo=dotnet&logoColor=white)](G3AspNet.csproj)
[![SQLite](https://img.shields.io/badge/Base%20de%20datos-SQLite-003B57?logo=sqlite&logoColor=white)](appsettings.json)
[![Identity](https://img.shields.io/badge/Auth-ASP.NET%20Core%20Identity-512BD4)](Program.cs)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3.3-7952B3?logo=bootstrap&logoColor=white)](wwwroot/lib/bootstrap)
[![Tests](https://img.shields.io/badge/Tests-xUnit-5C2D91)](tests/G3AspNet.Tests/G3AspNet.Tests.csproj)
[![Docker](https://img.shields.io/badge/Docker-disponible-2496ED?logo=docker&logoColor=white)](Dockerfile)
[![Estado](https://img.shields.io/badge/Estado-en%20desarrollo-orange)](#base-de-datos-y-datos-de-ejemplo)

Aplicación web para gestionar un taller mecánico: clientes y vehículos, reparaciones, almacén de recambios y un área privada para clientes. Proyecto académico del Grupo 3.

> [!WARNING]
> La aplicación está en desarrollo. Cada arranque elimina y recrea la base de datos. No uses una base con datos que necesites conservar.

## Funcionalidades

- Registro, inicio de sesión con usuario o email, perfil con avatar, cambio de contraseña y tema claro u oscuro.
- Verificación en dos pasos (TOTP) con código QR, bloqueo de cuentas y límite de peticiones en el acceso.
- Administración de cuentas: búsqueda, alta, edición, asignación de rol, activación/desactivación y baja.
- Vehículos con propietario. Un cliente solo ve los suyos.
- Reparaciones con mecánico, tipo, horas y tarifa, y un flujo de estados desde pendiente hasta entregada. El coste suma mano de obra y piezas.
- Reparaciones cerradas protegidas: solo un administrador puede modificarlas, y confirmándolo.
- Catálogo de recambios con stock, stock mínimo y aviso de stock bajo.
- Piezas por reparación con ajuste automático de stock y precio unitario histórico.
- Protección del historial: no se borran vehículos con reparaciones ni recambios ya usados.
- Autorización por rol y por recurso, y páginas de error propias.

Detalle completo en la [guía funcional](docs/guia-funcional.md).

## Perfiles

| Perfil | Resumen |
|---|---|
| `Admin` | Gestiona cuentas, roles y todo el taller. |
| `Mechanic` | Gestiona vehículos y recambios, y trabaja las reparaciones que tiene asignadas. |
| `Customer` | Consulta sus vehículos, sus reparaciones y el catálogo de recambios. |
| `User` | Rol asignado al registrarse. Gestiona su perfil y consulta el catálogo hasta que un administrador le asigne otro. |

La tabla de permisos está en la [guía funcional](docs/guia-funcional.md#permisos-por-perfil).

## Stack

| Componente | Detalle |
|---|---|
| Plataforma | .NET 10 (`net10.0`), ASP.NET Core MVC con vistas Razor |
| Acceso a datos | Entity Framework Core 10.0.11 con SQLite |
| Autenticación | ASP.NET Core Identity, con cookies y 2FA TOTP (QR generado con QRCoder 1.9.0) |
| Interfaz | Bootstrap 5.3.3, jQuery 3.7.1 y Font Awesome 6.7.2, servidos desde `wwwroot/lib`; tipografías de Google Fonts |
| Pruebas | xUnit 2.9.3, unitarias y de integración |
| Entrega | Docker y GitHub Actions |

## Requisitos

- SDK de .NET 10. [`global.json`](global.json) fija `10.0.100` y admite versiones posteriores de la misma línea (`rollForward: latestFeature`).
- Git.
- Docker, opcional.

Para preparar el entorno, consulta la [guía de IDE](docs/IDE-SETUP.md).

## Instalación y ejecución

```bash
git clone https://github.com/certidevs/g3_aspnet.git
cd g3_aspnet
dotnet restore G3AspNet.sln
dotnet run --project G3AspNet.csproj --launch-profile http
```

Abre <http://localhost:5107>. El perfil `http` usa el entorno `Development`. También existe el perfil `https`, en `https://localhost:7022`.

Para compilar y ejecutar las pruebas:

```bash
dotnet build G3AspNet.sln
dotnet test G3AspNet.sln
```

### Docker

```bash
docker build -t g3-aspnet .
docker run --rm -p 8080:8080 g3-aspnet
```

El contenedor escucha en el puerto `8080`, se ejecuta en entorno `Production`, carga las cuentas de demostración y no configura persistencia para SQLite. No lo expongas en un entorno compartido. Fuera de `Development` la cookie de sesión exige HTTPS: lee la [guía de despliegue](docs/despliegue.md) antes de usarlo.

## Configuración

Los valores por defecto están en [`appsettings.json`](appsettings.json). Se pueden sustituir con variables de entorno, usando `__` como separador.

| Clave | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | Configura la conexión SQLite; consulta la [guía de configuración](docs/configuracion.md). |
| `SeedData:Enabled` | Activa o desactiva la carga de roles, cuentas y datos demo. |
| `FileUploads:Images:MaxFileSizeBytes` | Límite del tamaño de las imágenes subidas. |
| `FileUploads:Images:AllowedExtensions` | Extensiones admitidas para las imágenes. |

La configuración puede sobrescribirse mediante variables de entorno con `__` como separador. No incluyas valores sensibles en el repositorio ni en la documentación.

<!-- TODO: la aplicación no usa `dotnet user-secrets` ni otro gestor de secretos. Documentarlo si se incorpora. -->

## Base de datos y datos de ejemplo

El proyecto no usa migraciones de EF Core. Al arrancar, [`DbInitializer`](Data/DbInitializer.cs) ejecuta `EnsureDeleted` y `EnsureCreated`, de modo que el esquema se genera a partir de las entidades y los datos se pierden en cada inicio. Esto ocurre aunque `SeedData:Enabled` sea `false`.

Con `SeedData:Enabled` en `true` (valor por defecto, en cualquier entorno), el arranque carga los cuatro roles, cuentas de ejemplo, vehículos, recambios y reparaciones. Las cuentas y el guion de demostración están en [`docs/demo.md`](docs/demo.md).

Con el seed desactivado la base queda vacía y sin roles, por lo que el registro de cuentas no funciona.

Para el esquema, consulta el [modelo de datos](docs/diagrama-er.md).

## Estructura de carpetas

```text
Authorization/          Políticas y handlers de autorización por recurso
Controllers/            Acciones MVC
Data/                   ApplicationDbContext y DbInitializer
Models/                 Entidades y enums
Services/               Cuentas de usuario y almacenamiento de avatares
Utilities/              Claims del usuario, QR y confirmación de reparaciones cerradas
ViewModels/             Modelos de formularios y páginas
Views/                  Vistas Razor, agrupadas por controlador
wwwroot/                CSS, JavaScript, imágenes y librerías locales
tests/G3AspNet.Tests/   Pruebas xUnit
docs/                   Documentación
scripts/                Utilidades del repositorio (publicación del wiki)
.github/workflows/      Integración continua
Dockerfile              Imagen del contenedor
```

## Documentación

Material para **presentaciones con empresas y consultores** (p. ej. Fundación Adecco): [dossier del proyecto](docs/presentacion-adecco.md) (guion de 45 minutos, outline de diapositivas y deck Marp enlazados desde allí).

| Documento | Contenido |
|---|---|
| [Presentación externa](docs/presentacion-adecco.md) | Dossier ejecutivo, empleabilidad, roadmap y FAQ. |
| [Guía funcional](docs/guia-funcional.md) | Perfiles, permisos y reglas de negocio. |
| [Modelo de datos](docs/diagrama-er.md) | Diagrama ER, relaciones, entidades y enums. |
| [Primeros pasos](docs/primeros-pasos.md) | Requisitos, restauración, ejecución y pruebas locales. |
| [Arquitectura](docs/arquitectura.md) | Estructura MVC, inyección de dependencias y pipeline HTTP. |
| [Base de datos y migraciones](docs/base-de-datos-y-migraciones.md) | SQLite, relaciones y ciclo de vida de la base de datos. |
| [Autenticación y roles](docs/autenticacion-y-roles.md) | Identity, cookies, 2FA y autorización por rol y recurso. |
| [Controladores y vistas](docs/controladores-y-vistas.md) | Acciones MVC, vistas Razor y recursos frontend. |
| [Configuración](docs/configuracion.md) | Claves de configuración y variables de entorno, sin valores sensibles. |
| [Testing y CI](docs/testing-y-ci.md) | Suite xUnit y workflow de integración continua. |
| [Módulo de reparaciones](docs/documentacion-tecnica-reparaciones.md) | Estados, autorización y código del módulo. |
| [Despliegue](docs/despliegue.md) | Ejecución con Docker y límites conocidos del despliegue. |
| [Demostración](docs/demo.md) | Cuentas de ejemplo y guion. |
| [Problemas frecuentes](docs/problemas-frecuentes.md) | Errores habituales y soluciones. |
| [Guía de contribución](CONTRIBUTING.md) | Ramas, pull requests y reglas de código. |
| [Guía de IDE](docs/IDE-SETUP.md) | Preparación del entorno de desarrollo. |
| [Dependencias](docs/DEPENDENCIAS.md) | Registro de paquetes y su revisión. |
| [Planning](docs/planning.md) | Estado, errores conocidos y reparto del trabajo. |

## Equipo

Grupo 3:

- [Iván López](https://github.com/irilopa)
- [Rafa-Gamero](https://github.com/Rafa-Gamero)
- [ricardohsm1996](https://github.com/ricardohsm1996)
- [rubogar](https://github.com/rubogar)

Con aportaciones de [Alan Sastre](https://github.com/alansastre) y [M. del Olmo](https://github.com/mariadelolmo-cdevs).

## Licencia

<!-- TODO: el repositorio no declara licencia. Añadir un archivo `LICENSE` y citarlo aquí. -->
Sin licencia declarada.
