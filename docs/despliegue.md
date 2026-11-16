# Despliegue

El repositorio incluye un `Dockerfile` multi-stage: compila y publica con la imagen SDK de .NET 10 y ejecuta el resultado con el runtime de ASP.NET Core 10.

Documentos relacionados: [Configuración](configuracion.md) · [Base de datos y migraciones](base-de-datos-y-migraciones.md) · [Problemas frecuentes](problemas-frecuentes.md).

## Ejecutar con Docker

Desde la raíz del repositorio:

```bash
docker build -t g3-aspnet .
docker run --rm -p 10000:10000 g3-aspnet
```

Abre <http://localhost:10000>. El proceso escucha en el puerto indicado por `PORT` o, si no está definido, en `10000`.

## Persistencia y datos demo

El contenedor crea los directorios `App_Data` y `wwwroot/uploads/avatars`, pero no configura volúmenes persistentes para SQLite. Además, el inicializador ejecuta `EnsureDeleted` y `EnsureCreated` en cada arranque, por lo que la base configurada se elimina y recrea. **El despliegue actual es apropiado para desarrollo/demo, no para conservar datos.**

No se documenta un entorno productivo, hosting gestionado ni configuración de volúmenes. **TODO: confirmar** el destino de despliegue y la estrategia de persistencia antes de describir un despliegue productivo. Para ejecutar la aplicación localmente sin Docker, consulta [Primeros pasos](primeros-pasos.md).
