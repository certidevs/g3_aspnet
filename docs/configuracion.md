# Configuración

La configuración se carga desde `appsettings.json` y, en desarrollo, `appsettings.Development.json`. Los perfiles `http` y `https` de `Properties/launchSettings.json` seleccionan `Development` al ejecutarse localmente.

Documentos relacionados: [Primeros pasos](primeros-pasos.md) · [Autenticación y roles](autenticacion-y-roles.md) · [Despliegue](despliegue.md).

## Claves utilizadas

| Clave o variable | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | Configura la conexión SQLite utilizada por `ApplicationDbContext`. No se documenta aquí su valor. |
| `SeedData:Enabled` | Activa o desactiva la creación de roles y datos demo; vale `true` por defecto en todos los entornos. Desactivado, la base queda vacía y sin roles. No evita que la base se elimine al iniciar. |
| `FileUploads:Images:MaxFileSizeBytes` | Límite del tamaño de una imagen subida. |
| `FileUploads:Images:AllowedExtensions` | Extensiones permitidas para las imágenes. |
| `Logging:LogLevel` | Niveles de registro de la aplicación y de ASP.NET Core. |
| `AllowedHosts` | Configuración estándar de hosts permitidos. |
| `ASPNETCORE_ENVIRONMENT` | Selecciona el entorno de ejecución; los perfiles locales usan `Development`. |
| `ASPNETCORE_HTTP_PORTS` | Puertos HTTP en el contenedor; el Dockerfile fija `8080`. |

ASP.NET Core permite sustituir claves de configuración con variables de entorno usando `__` en lugar de `:`. Por ejemplo, la clave `SeedData:Enabled` corresponde al nombre `SeedData__Enabled`; esta guía no incluye valores de configuración ni cadenas de conexión.

## Valores fijados en el código

Estas opciones no se leen de la configuración; están en [`Program.cs`](../Program.cs): reglas de contraseña, bloqueo de cuentas, cookie, intervalo de revalidación de sesión y límite de peticiones. Sus valores están en [Autenticación y roles](autenticacion-y-roles.md).

## Secretos y archivos locales

No se ha configurado `dotnet user-secrets` en el proyecto (no hay `UserSecretsId`). `appsettings.Development.json` solo establece niveles de logging. No añadas contraseñas, tokens ni valores sensibles a archivos versionados o a la wiki; usa un gestor de secretos apropiado al entorno cuando sea necesario.

Para comprender el impacto de `SeedData:Enabled` y el comportamiento al arrancar, consulta [Base de datos y migraciones](base-de-datos-y-migraciones.md).
