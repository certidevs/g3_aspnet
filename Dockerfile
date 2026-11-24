# syntax=docker/dockerfile:1

# Imagen de SDK solo para restaurar y publicar. No llega a producción.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restaurar primero el csproj para cachear NuGet si el código cambia y las dependencias no.
COPY --link G3AspNet.csproj ./
RUN dotnet restore ./G3AspNet.csproj -a $TARGETARCH

COPY --link . .
RUN dotnet publish ./G3AspNet.csproj \
    -c Release \
    -a $TARGETARCH \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime: ASP.NET, sin SDK, sin herramientas de desarrollo.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Puerto HTTP por defecto de las imágenes oficiales de .NET 8+.
EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# SQLite (App_Data) y avatares necesitan escritura; el usuario app (APP_UID=1654) no es root.
RUN mkdir -p /app/App_Data /app/wwwroot/uploads/avatars \
    && chown -R $APP_UID:$APP_UID /app/App_Data /app/wwwroot/uploads

USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "G3AspNet.dll"]
