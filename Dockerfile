# syntax=docker/dockerfile:1

# La distribución se fija (Ubuntu 24.04 "noble") para que no cambie sin avisar.
# Los parches de .NET y del sistema llegan al reconstruir con: docker build --pull
ARG DOTNET_VERSION=10.0

# Imagen de SDK solo para restaurar y publicar. No llega a producción.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
ARG TARGETARCH
WORKDIR /src

# Restaurar primero el csproj para cachear NuGet si el código cambia y las dependencias no.
# global.json obliga a compilar con la misma línea de SDK que en local y en CI.
COPY --link global.json G3AspNet.csproj ./
RUN dotnet restore ./G3AspNet.csproj -a $TARGETARCH

COPY --link . .
RUN dotnet publish ./G3AspNet.csproj \
    -c Release \
    -a $TARGETARCH \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime: ASP.NET, sin SDK, sin herramientas de desarrollo.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble AS final
LABEL org.opencontainers.image.title="G3AspNet" \
      org.opencontainers.image.source="https://github.com/certidevs/g3_aspnet"
WORKDIR /app

# Los binarios quedan como propiedad de root: el proceso puede leerlos, pero no modificarlos.
COPY --link --from=build /app/publish .

# Únicas carpetas con escritura para el usuario app (APP_UID=1654, sin privilegios):
# la base SQLite, los avatares y las claves que cifran las cookies y los tokens antiforgery.
RUN install -d -o $APP_UID -g $APP_UID \
    /app/App_Data \
    /app/wwwroot/uploads/avatars \
    /home/app/.aspnet \
    /home/app/.aspnet/DataProtection-Keys

# Se conservan los datos y no se cargan las cuentas demo, que tienen contraseñas públicas.
# El primer administrador se crea con las variables AdminAccount__*; ver docs/despliegue.md.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    Database__RecreateOnStartup=false \
    SeedData__Enabled=false

# Puerto HTTP por defecto de las imágenes oficiales de .NET 8+. TLS lo termina un proxy.
EXPOSE 8080

# Se usa el UID numérico para que el orquestador pueda comprobar que no es root.
USER $APP_UID

# La imagen no trae curl: bash pide la portada y exige una respuesta correcta.
# Comprueba que la aplicación responde; no garantiza el estado de la base de datos.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/${ASPNETCORE_HTTP_PORTS%%;*} && printf 'GET / HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -Eq ' (200|30[1278]) '"]

ENTRYPOINT ["dotnet", "G3AspNet.dll"]
