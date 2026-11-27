# Control de dependencias - G3AspNet

Registro de las dependencias del proyecto, su versión, su propósito y su estado de revisión.

**Última revisión:** 2026-11-27 (contraste con los `.csproj` y `wwwroot/lib`)
**Runtime:** .NET 10.0 (`net10.0`)
**SDK:** 10.0.100 (`global.json`, `rollForward: latestFeature`, sin preliminares)

## Esquema de base de datos

Este proyecto **no utiliza migraciones de EF Core**. Al iniciar en `Development`, `DbInitializer`
elimina la base de datos y la vuelve a crear desde las entidades (`EnsureDeleted` y
`EnsureCreated`); fuera de ese entorno solo crea el esquema si no existe y conserva los datos.
Los cambios de esquema se realizan en las entidades y en la configuración de `ApplicationDbContext`,
no mediante `dotnet ef migrations add` ni `dotnet ef database update`.

---

## 1. Dependencias de la aplicación (`G3AspNet.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | Runtime | ORM EF Core con base de datos SQLite | Activa | Debe coincidir con la versión de `Design` e `Identity.EntityFrameworkCore` |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Desarrollo | Herramientas de diseño de EF Core | Activa | `PrivateAssets=all`, no se distribuye. El proyecto no usa migraciones |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | Runtime | Login, cookies, contraseñas y roles | Activa | Persistencia de Identity con EF Core |
| `QRCoder` | 1.9.0 | Runtime | Código QR en SVG para configurar el 2FA | Activa | Licencia MIT. Se genera en el servidor. Arrastra `System.Drawing.Common` 6.0.0 |
| `QuestPDF` | 2026.9.2 | Runtime | Facturas en PDF | Activa | [Licencia Community](https://www.questpdf.com/license/) con condiciones (entre ellas, ingresos anuales bajo 1 M USD en empresas). No es una licencia OSI |

## 2. Dependencias de pruebas (`tests/G3AspNet.Tests/G3AspNet.Tests.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.0 | Pruebas | Arranque en memoria para las pruebas de integración | Activa | |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Pruebas | Infraestructura para `dotnet test` | Activa | |
| `xunit` | 2.9.3 | Pruebas | Framework de pruebas unitarias | Activa | |
| `xunit.runner.visualstudio` | 3.1.4 | Pruebas | Adaptador para Rider y Visual Studio | Activa | |

**Referencia de proyecto:** `G3AspNet.Tests` → `G3AspNet.csproj`

## 2.1. Librerías de front (`wwwroot/lib`)

| Librería | Versión | Propósito | Notas |
|---|---|---|---|
| Bootstrap | 5.3.3 | Estilos y componentes | |
| jQuery | 3.7.1 | Dependencia de la validación en el cliente | |
| jQuery Validation | 1.21.0 | Validación en el cliente | |
| jQuery Validation Unobtrusive | 4.0.0 | Enlace con las anotaciones de los view models | |
| Font Awesome Free | 6.7.2 | Iconos | Solo `css/all.min.css` y `webfonts/`; antes se cargaba por CDN |

La layout carga además las tipografías Oswald y Roboto desde Google Fonts (CDN).

## 2.2. Herramientas locales (`.config/dotnet-tools.json`)

| Herramienta | Versión | Notas |
|---|---|---|
| `dotnet-ef` | 10.0.11 | Declarada, pero el proyecto no usa migraciones |

---

## 3. Historial de cambios

| Fecha | Paquete | De | A | Motivo | Responsable |
|---|---|---|---|---|---|
| 2026-10-19 | Todos | - | - | Alta inicial del registro | |
| 2026-11-26 | `QRCoder` | - | 1.9.0 | QR para configurar el 2FA | |
| 2026-11-27 | `QuestPDF` | - | 2026.9.2 | Facturas en PDF | |

---

## 4. Procedimiento de revisión

Ejecutar desde la raíz del proyecto:

```bash
# Paquetes con versiones nuevas disponibles
dotnet list package --outdated

# Paquetes con vulnerabilidades conocidas
dotnet list package --vulnerable --include-transitive

# Paquetes deprecados
dotnet list package --deprecated

# Restaurar y compilar tras cualquier cambio
dotnet restore
dotnet build
dotnet test
```

### Checklist al actualizar una dependencia

- [ ] Revisar las notas de la versión (release notes) y los cambios que rompen compatibilidad.
- [ ] Mantener alineadas las versiones de `Microsoft.EntityFrameworkCore.*` y `Microsoft.AspNetCore.Identity.*` (misma versión mayor y menor).
- [ ] Actualizar la versión en el `.csproj` correspondiente.
- [ ] Ejecutar `dotnet build` y `dotnet test` sin errores.
- [ ] Si cambia el esquema, actualizar las entidades y `ApplicationDbContext`; no crear ni aplicar migraciones. Fuera de `Development` la base se conserva y `EnsureCreated` no actualiza su esquema.
- [ ] Anotar el cambio en la sección 3 de este documento.

### Frecuencia recomendada

- **Mensual:** `dotnet list package --vulnerable` y `--outdated`.
- **Por cada parche de seguridad de .NET:** actualizar de inmediato.
- **Cada versión mayor de .NET:** revisar el `global.json` y todos los `TargetFramework`. 
