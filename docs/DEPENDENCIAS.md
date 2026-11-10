# Control de dependencias - G3AspNet

Registro de las dependencias del proyecto, su versión, su propósito y su estado de revisión.

**Última revisión:** 2026-10-19
**Runtime:** .NET 10.0 (`net10.0`)
**SDK:** 10.0.100 (`global.json`, `rollForward: latestFeature`, sin preliminares)

## Esquema de base de datos

Este proyecto **no utiliza migraciones de EF Core**. Al iniciar la aplicación, `DbInitializer`
elimina la base de datos y la vuelve a crear desde las entidades (`EnsureDeleted` y
`EnsureCreated`); por tanto, los datos existentes se pierden en cada arranque. Los cambios de
esquema se realizan en las entidades y en la configuración de `ApplicationDbContext`, no mediante
`dotnet ef migrations add` ni `dotnet ef database update`.

---

## 1. Dependencias de la aplicación (`G3AspNet.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | Runtime | ORM EF Core con base de datos SQLite | Activa | Debe coincidir con la versión de `Design` e `Identity.EntityFrameworkCore` |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Desarrollo | Herramientas de diseño de EF Core | Activa | `PrivateAssets=all`, no se distribuye. El proyecto no usa migraciones |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | Runtime | Login, cookies, contraseñas y roles | Activa | Persistencia de Identity con EF Core |

## 2. Dependencias de pruebas (`tests/G3AspNet.Tests/G3AspNet.Tests.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Pruebas | Infraestructura para `dotnet test` | Activa | |
| `xunit` | 2.9.3 | Pruebas | Framework de pruebas unitarias | Activa | |
| `xunit.runner.visualstudio` | 3.1.4 | Pruebas | Adaptador para Rider y Visual Studio | Activa | |

**Referencia de proyecto:** `G3AspNet.Tests` → `G3AspNet.csproj`

## 2.1. Librerías de front (`wwwroot/lib`)

| Librería | Versión | Propósito | Notas |
|---|---|---|---|
| Font Awesome Free | 6.7.2 | Iconos | Solo `css/all.min.css` y `webfonts/`; antes se cargaba por CDN |

---

## 3. Historial de cambios

| Fecha | Paquete | De | A | Motivo | Responsable |
|---|---|---|---|---|---|
| 2026-10-19 | Todos | - | - | Alta inicial del registro | |

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
- [ ] Si cambia el esquema, actualizar las entidades y `ApplicationDbContext`; no crear ni aplicar migraciones. Tener en cuenta que `DbInitializer` recrea la base de datos y borra sus datos en cada arranque.
- [ ] Anotar el cambio en la sección 3 de este documento.

### Frecuencia recomendada

- **Mensual:** `dotnet list package --vulnerable` y `--outdated`.
- **Por cada parche de seguridad de .NET:** actualizar de inmediato.
- **Cada versión mayor de .NET:** revisar el `global.json` y todos los `TargetFramework`. 
