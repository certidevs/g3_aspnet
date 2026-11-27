# Configuración

La configuración se carga desde `appsettings.json` y, en desarrollo, `appsettings.Development.json`. Los perfiles `http` y `https` de `Properties/launchSettings.json` seleccionan `Development` al ejecutarse localmente.

Documentos relacionados: [Primeros pasos](primeros-pasos.md) · [Autenticación y roles](autenticacion-y-roles.md) · [Despliegue](despliegue.md).

## Claves utilizadas

| Clave o variable | Uso |
|---|---|
| `ConnectionStrings:DefaultConnection` | Configura la conexión SQLite utilizada por `ApplicationDbContext`. No se documenta aquí su valor. |
| `Database:RecreateOnStartup` | Borra y recrea la base al arrancar. `false` en `appsettings.json` y en la imagen Docker; `true` en `Development`. |
| `SeedData:Enabled` | Carga las cuentas y datos demo en una base sin cuentas. `false` en `appsettings.json` y en la imagen Docker; `true` en `Development`. Los roles se crean siempre. |
| `AdminAccount:UserName`, `AdminAccount:Email`, `AdminAccount:Password` | Crean el [administrador inicial](base-de-datos-y-migraciones.md#administrador-inicial). No los guardes en archivos versionados. |
| `FileUploads:Images:MaxFileSizeBytes` | Límite del tamaño de una imagen subida. |
| `FileUploads:Images:AllowedExtensions` | Extensiones permitidas para las imágenes. |
| `Invoice:VatRate` | Porcentaje de IVA que se suma en la factura. |
| `Invoice:IssuerName`, `Invoice:IssuerTaxId`, `Invoice:IssuerAddress` | Datos del taller que aparecen en la factura; los vacíos no se imprimen. |
| `Logging:LogLevel` | Niveles de registro de la aplicación y de ASP.NET Core. |
| `AllowedHosts` | Configuración estándar de hosts permitidos. |
| `ASPNETCORE_ENVIRONMENT` | Selecciona el entorno de ejecución; los perfiles locales usan `Development`. |
| `ASPNETCORE_HTTP_PORTS` | Puertos HTTP en el contenedor; el Dockerfile fija `8080`. |

ASP.NET Core permite sustituir claves de configuración con variables de entorno usando `__` en lugar de `:`. Por ejemplo, la clave `SeedData:Enabled` corresponde al nombre `SeedData__Enabled`; esta guía no incluye valores de configuración ni cadenas de conexión.

## Valores fijados en el código

Estas opciones no se leen de la configuración; están en [`Program.cs`](../Program.cs): reglas de contraseña, bloqueo de cuentas, cookie, intervalo de revalidación de sesión y límite de peticiones. Sus valores están en [Autenticación y roles](autenticacion-y-roles.md).

## Secretos y archivos locales

No se ha configurado `dotnet user-secrets` en el proyecto (no hay `UserSecretsId`). `appsettings.Development.json` activa la recreación de la base y los datos demo, y ajusta los niveles de logging. No añadas contraseñas, tokens ni valores sensibles a archivos versionados o a la wiki; usa un gestor de secretos apropiado al entorno cuando sea necesario.

Para comprender el impacto de `SeedData:Enabled` y el comportamiento al arrancar, consulta [Base de datos y migraciones](base-de-datos-y-migraciones.md).
