# Testing y CI

Las pruebas automatizadas están en `tests/G3AspNet.Tests/`, un proyecto xUnit que referencia `G3AspNet.csproj`.

Documentos relacionados: [Primeros pasos](primeros-pasos.md) · [Guía de contribución](../CONTRIBUTING.md) · [Dependencias](DEPENDENCIAS.md).

## Ejecutar localmente

Desde la raíz del repositorio:

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln
dotnet test G3AspNet.sln
```

## Qué cubre la suite

| Tipo | Ubicación | Cubre |
|---|---|---|
| Unitarias | `tests/G3AspNet.Tests/` | Lógica de dominio, relaciones, almacenamiento de imágenes, modelo de login y autorización de piezas usadas. |
| Integración | `tests/G3AspNet.Tests/Integration/` | Cuentas, perfil, administración de usuarios, vehículos, recambios, reparaciones, piezas, reparaciones cerradas, portada, datos de ejemplo, arranque de la base y regresiones de seguridad. |

Las pruebas de integración arrancan la aplicación en memoria con `WebApplicationFactory` ([`AppFactory`](../tests/G3AspNet.Tests/Integration/AppFactory.cs)). Cada fábrica usa una base SQLite temporal, que se borra al terminar, y carga los datos de ejemplo. No tocan `App_Data/g3.db`.

Para ejecutar solo una parte:

```bash
dotnet test tests/G3AspNet.Tests/G3AspNet.Tests.csproj --filter "FullyQualifiedName~Integration"
```

## Integración continua

El workflow [`build-and-test.yml`](../.github/workflows/build-and-test.yml) se ejecuta en `push` y `pull_request` a `main`, y también admite ejecución manual. Usa `ubuntu-latest` y .NET 10. En Release restaura la solución, compila sin restaurar de nuevo y ejecuta las pruebas sin recompilar.

Para reproducir ese flujo:

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln --configuration Release --no-restore
dotnet test G3AspNet.sln --configuration Release --no-build --verbosity normal
```

## Datos de prueba

Las pruebas no deben depender de datos productivos. Usa los métodos de `AppFactory` (`CreateVehicleAsync`, `CreateRepairAsync`, `CreateSparePartAsync`) para preparar los datos de cada prueba.
