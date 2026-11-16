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

La solución incluye pruebas para lógica de dominio, relaciones, modelos de vista y resultados de carga de imágenes.

## Integración continua

El workflow [`build-and-test.yml`](../.github/workflows/build-and-test.yml) se ejecuta en `push` y `pull_request` a `main`, y también admite ejecución manual. Usa `ubuntu-latest` y .NET 10. En Release restaura la solución, compila sin restaurar de nuevo y ejecuta las pruebas sin recompilar.

Para reproducir ese flujo:

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln --configuration Release --no-restore
dotnet test G3AspNet.sln --configuration Release --no-build --verbosity normal
```

## Datos de prueba

Las pruebas de modelos y relaciones no deben depender de datos productivos. La aplicación principal recrea su SQLite al iniciar; consulta [Base de datos y migraciones](base-de-datos-y-migraciones.md) antes de ejecutar la aplicación con una base local que quieras conservar.
