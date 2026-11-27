# Despliegue

El repositorio incluye un `Dockerfile` multi-stage: restaura y publica con la imagen SDK de .NET 10 y ejecuta el resultado con el runtime de ASP.NET Core 10.

Documentos relacionados: [Configuración](configuracion.md) · [Base de datos y migraciones](base-de-datos-y-migraciones.md) · [Problemas frecuentes](problemas-frecuentes.md).

## Ejecutar con Docker

Desde la raíz del repositorio:

```bash
docker build -t g3-aspnet .
docker run --rm -p 8080:8080 g3-aspnet
```

Abre <http://localhost:8080>.

| Aspecto | Valor en la imagen |
|---|---|
| Puerto HTTP | `8080` (`ASPNETCORE_HTTP_PORTS`) |
| Entorno | `Production` (`ASPNETCORE_ENVIRONMENT`) |
| Usuario | Sin privilegios (`$APP_UID`) |
| Carpetas con escritura | `/app/App_Data` y `/app/wwwroot/uploads` |
| Excluido de la imagen | `tests/`, `docs/`, archivos `.md` y bases de datos locales |

## Limitaciones

- **HTTPS.** En `Production` la cookie de sesión es `Secure` y se activa HSTS. La imagen solo sirve HTTP, así que para iniciar sesión fuera de `localhost` hace falta un proxy que termine TLS. La aplicación no configura cabeceras reenviadas (`ForwardedHeaders`): detrás de un proxy, la aplicación ve la petición como HTTP y el límite de peticiones se aplica a la IP del proxy.
- **Cuentas de demostración.** `SeedData:Enabled` vale `true` por defecto, de modo que la imagen crea las cuentas de ejemplo también en `Production`. No expongas la imagen así en un entorno compartido. Se puede desactivar con `-e SeedData__Enabled=false`, pero entonces no se crean los roles ni ninguna cuenta y el registro falla.
- **Persistencia.** No hay volúmenes para SQLite y `DbInitializer` ejecuta `EnsureDeleted` y `EnsureCreated` en cada arranque. **El despliegue actual es apropiado para desarrollo y demo, no para conservar datos.**

No se documenta un entorno productivo ni un hosting gestionado. **TODO: confirmar** el destino de despliegue y la estrategia de persistencia antes de describir un despliegue productivo. Para ejecutar la aplicación sin Docker, consulta [Primeros pasos](primeros-pasos.md).
