# Preparar el entorno de C# para G3 ASP.NET

Esta guía instala **Visual Studio Code**, la extensión **C# Dev Kit**, el **SDK de .NET 10** y Git en Windows, macOS o Linux. El SDK incluye el entorno necesario para desarrollar y ejecutar aplicaciones ASP.NET Core; no hace falta instalar SQLite ni Entity Framework por separado.

El proyecto usa `net10.0`. El archivo `global.json` solicita el SDK `10.0.100` y permite una versión posterior compatible de la misma línea mediante `latestFeature`.

## Windows

En PowerShell, con [WinGet](https://learn.microsoft.com/windows/package-manager/winget/) instalado:

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact
winget install --id Microsoft.VisualStudioCode --exact
winget install --id Git.Git --exact
```

Cierra y vuelve a abrir PowerShell para actualizar el `PATH`. Instala la extensión:

```powershell
code --install-extension ms-dotnettools.csdevkit
```

Si `code` no se reconoce, reinicia la sesión o instala **C# Dev Kit** desde el panel **Extensions** de VS Code. También puedes usar los instaladores oficiales de [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), [VS Code](https://code.visualstudio.com/download) y [Git](https://git-scm.com/downloads/win).

## macOS

Con [Homebrew](https://brew.sh/) instalado, abre Terminal y ejecuta:

```bash
brew install --cask dotnet-sdk
brew install --cask visual-studio-code
brew install git
```

El paquete `dotnet-sdk` instala la versión disponible en Homebrew, que puede cambiar. Comprueba después que tienes un SDK 10 compatible; si no, instala el SDK 10 desde la [página oficial de .NET](https://dotnet.microsoft.com/download/dotnet/10.0).

Abre VS Code y ejecuta **Shell Command: Install 'code' and optionally 'copilot' commands in PATH** desde la paleta de comandos (`Cmd+Shift+P`). No es necesario instalar Copilot. Abre una terminal nueva e instala la extensión:

```bash
code --install-extension ms-dotnettools.csdevkit
```

Si no dispones de Homebrew, utiliza los instaladores oficiales de [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) y [VS Code](https://code.visualstudio.com/download); Git también está disponible con las herramientas de línea de comandos de Xcode (`xcode-select --install`).

## Linux

### Ubuntu 26.04

En una terminal, instala el SDK, Git y VS Code mediante APT y Snap:

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 git
sudo snap install --classic code
code --install-extension ms-dotnettools.csdevkit
```

El último comando de instalación de VS Code requiere que Snap esté disponible. Si prefieres otro método, consulta la [guía de instalación de VS Code en Linux](https://code.visualstudio.com/docs/setup/linux).

Para otras versiones de Ubuntu o distribuciones Linux, sigue las instrucciones de [.NET para Linux](https://learn.microsoft.com/dotnet/core/install/linux) y de [VS Code para Linux](https://code.visualstudio.com/docs/setup/linux), ya que los paquetes y repositorios dependen de la distribución. Después ejecuta `code --install-extension ms-dotnettools.csdevkit`.

## Comprobar la instalación

Desde una terminal nueva, comprueba los comandos y la extensión:

```bash
dotnet --list-sdks
git --version
code --version
code --list-extensions
```

La salida de `dotnet --list-sdks` debe incluir un SDK 10 compatible y la de `code --list-extensions`, `ms-dotnettools.csdevkit`. Si `code` no está en el `PATH`, abre VS Code e instala la extensión desde **Extensions**. C# Dev Kit puede requerir que inicies sesión; consulta la [guía oficial](https://code.visualstudio.com/docs/csharp/signing-in).

## Abrir y ejecutar el proyecto

En PowerShell o en una terminal:

```bash
git clone https://github.com/certidevs/g3_aspnet.git
cd g3_aspnet
dotnet --version
dotnet restore G3AspNet.sln
code .
```

Ejecuta `dotnet --version` dentro de la carpeta del repositorio para comprobar que `global.json` selecciona un SDK compatible. En VS Code, abre la carpeta `g3_aspnet` y acepta la recomendación de C# Dev Kit si aparece; la solución es `G3AspNet.sln`.

Para iniciar la aplicación desde la terminal integrada:

```bash
dotnet run --project G3AspNet.csproj --launch-profile http
```

Abre <http://localhost:5107>. También puedes seleccionar el perfil `http` en VS Code y usar **Run and Debug** (`F5`). Las tareas **G3: compilar** y **G3: ejecutar tests** están disponibles en **Terminal → Run Task**.

> **Atención:** cada inicio elimina y recrea la base de datos configurada, incluso si se desactivan los datos demo. Consulta el [README](../README.md) para conocer la configuración, las pruebas y las demás limitaciones.

## Visual Studio 2026 para Windows

Como alternativa a VS Code, instala la carga de trabajo **ASP.NET and web development** en Visual Studio Installer, abre `G3AspNet.sln`, selecciona el perfil `http` y pulsa `F5`. Se usa la misma solución y configuración del proyecto.
