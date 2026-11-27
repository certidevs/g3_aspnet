# Despliegue

El repositorio incluye un `Dockerfile` multi-stage: restaura y publica con la imagen SDK de .NET 10 y ejecuta el resultado con el runtime de ASP.NET Core 10.

Documentos relacionados: [Configuración](configuracion.md) · [Base de datos y migraciones](base-de-datos-y-migraciones.md) · [Problemas frecuentes](problemas-frecuentes.md).

## Ejecutar con Docker

Desde la raíz del repositorio:

```bash
docker build --pull -t g3-aspnet .
```

`--pull` descarga los últimos parches de las imágenes base.

### Demo en local

```bash
docker run --rm -p 127.0.0.1:8080:8080 -e SeedData__Enabled=true g3-aspnet
```

Abre <http://localhost:8080>. Las cuentas están en [Demostración](demo.md). No publiques el puerto fuera de tu equipo con el seed activado.

### Detrás de un proxy con HTTPS

En el primer arranque, define el administrador. `read -rs` evita que la contraseña quede en el historial de la terminal:

```bash
export AdminAccount__UserName=gestor
export AdminAccount__Email=gestor@example.com
read -rs AdminAccount__Password && export AdminAccount__Password

docker run -d --name g3-aspnet --restart unless-stopped \
  -p 127.0.0.1:8080:8080 \
  -e AdminAccount__UserName -e AdminAccount__Email -e AdminAccount__Password \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  --read-only --tmpfs /tmp \
  --cap-drop=ALL --security-opt=no-new-privileges \
  -v g3-data:/app/App_Data \
  -v g3-avatars:/app/wwwroot/uploads/avatars \
  -v g3-keys:/home/app/.aspnet/DataProtection-Keys \
  g3-aspnet
```

Las variables de un contenedor se pueden leer con `docker inspect`. Cuando hayas iniciado sesión, activa la 2FA, elimina el contenedor y créalo de nuevo sin las tres variables `AdminAccount__*`. Los volúmenes conservan los datos.

`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` hace que la aplicación acepte las cabeceras `X-Forwarded-For` y `X-Forwarded-Proto` **de cualquier origen**. Actívalo solo si el contenedor es accesible únicamente a través del proxy.

## Qué incluye la imagen

| Aspecto | Valor |
|---|---|
| Imágenes base | .NET 10 sobre Ubuntu 24.04 (`10.0-noble`) |
| Puerto HTTP | `8080` (`ASPNETCORE_HTTP_PORTS`) |
| Entorno | `Production` (`ASPNETCORE_ENVIRONMENT`) |
| Base de datos | Se conserva (`Database__RecreateOnStartup=false`) |
| Datos de ejemplo | Desactivados (`SeedData__Enabled=false`) |
| Usuario | Sin privilegios (`$APP_UID`) |
| Binarios | Propiedad de `root`, de solo lectura para la aplicación |
| Carpetas con escritura | `/app/App_Data`, `/app/wwwroot/uploads/avatars` y las claves de Data Protection |
| Comprobación de salud | Pide la portada cada 30 segundos. No garantiza el estado de la base de datos |
| Excluido | `tests/`, `docs/`, `scripts/`, archivos de npm y de herramientas, y bases de datos locales |

El volumen de claves conserva la clave que cifra las cookies y los tokens antiforgery al recrear el contenedor. La clave se guarda sin cifrar: protege el acceso a ese volumen.

## Limitaciones

**Antes de un uso en producción, revisa estos puntos.**

- **Esquema.** No hay migraciones. Una versión que cambie las entidades no actualiza la base existente; consulta [Base de datos](base-de-datos-y-migraciones.md#migraciones).
- **Copias de seguridad.** No hay ningún proceso que copie la base ni los avatares. Planifica copias de los volúmenes.
- **HTTPS.** La imagen solo sirve HTTP. En `Production` la cookie de sesión es `Secure`, así que hace falta un proxy que termine TLS.
- **Una sola instancia.** SQLite, los avatares y el límite de peticiones son locales al contenedor.

No se documenta un hosting gestionado. **TODO: confirmar** el destino de despliegue. Para ejecutar la aplicación sin Docker, consulta [Primeros pasos](primeros-pasos.md).
