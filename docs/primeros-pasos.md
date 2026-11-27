# Primeros pasos

Guía para restaurar, compilar y ejecutar TALLER .NET en un entorno local.

Documentos relacionados: [Configuración](configuracion.md) · [Testing y CI](testing-y-ci.md) · [Problemas frecuentes](problemas-frecuentes.md).

## Requisitos

- SDK de .NET 10 compatible. [`global.json`](../global.json) solicita `10.0.100` y permite una versión posterior de la misma línea (`latestFeature`).
- Git.
- Docker, solo si se va a probar el contenedor.

## Descargar y ejecutar

Desde una terminal:

```bash
git clone https://github.com/certidevs/g3_aspnet.git
cd g3_aspnet
dotnet restore G3AspNet.sln
dotnet run --project G3AspNet.csproj --launch-profile http
```

El perfil `http` inicia la aplicación en <http://localhost:5107> con el entorno `Development`. También existe un perfil `https`; si el certificado local no es de confianza, consulta [Problemas frecuentes](problemas-frecuentes.md).

Al iniciar, la aplicación crea la carpeta de datos y ejecuta `DbInitializer`. **La base de datos se elimina y se crea de nuevo en cada arranque.** No uses datos que debas conservar.

## Compilar y probar

```bash
dotnet build G3AspNet.sln
dotnet test G3AspNet.sln
```

Las cuentas para iniciar sesión están en [Demostración](demo.md).

Para ejecutar el contenedor, sigue [Despliegue](despliegue.md). Para revisar las claves de configuración disponibles, consulta [Configuración](configuracion.md).
