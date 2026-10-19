# Control de dependencias - G3AspNet

Registro de las dependencias del proyecto, su versión, su propósito y su estado de revisión.

**Última revisión:** 2026-10-19
**Runtime:** .NET 10.0 (`net10.0`)
**SDK:** 10.0.100 (`global.json`, `rollForward: latestFeature`, sin preliminares)

---

## 1. Dependencias de la aplicación (`G3AspNet.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | Runtime | ORM EF Core con base de datos SQLite | Activa | Debe coincidir con la versión de `Design` e `Identity.EntityFrameworkCore` |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Desarrollo | Herramientas de migraciones (`dotnet ef`) | Activa | `PrivateAssets=all`, no se distribuye |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | Runtime | Login, cookies, contraseñas y roles | Activa | Persistencia de Identity con EF Core |

## 2. Dependencias de pruebas (`tests/G3AspNet.Tests/G3AspNet.Tests.csproj`)

| Paquete NuGet | Versión | Tipo | Propósito | Estado | Notas |
|---|---|---|---|---|---|
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Pruebas | Infraestructura para `dotnet test` | Activa | |
| `xunit` | 2.9.3 | Pruebas | Framework de pruebas unitarias | Activa | |
| `xunit.runner.visualstudio` | 3.1.4 | Pruebas | Adaptador para Rider y Visual Studio | Activa | |

**Referencia de proyecto:** `G3AspNet.Tests` → `G3AspNet.csproj`

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
- [ ] Si cambia el esquema de EF Core, revisar migraciones (`dotnet ef migrations add ...`).
- [ ] Anotar el cambio en la sección 3 de este documento.

### Frecuencia recomendada

- **Mensual:** `dotnet list package --vulnerable` y `--outdated`.
- **Por cada parche de seguridad de .NET:** actualizar de inmediato.
- **Cada versión mayor de .NET:** revisar el `global.json` y todos los `TargetFramework`.
