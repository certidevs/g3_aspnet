# Copilot instructions

## Build and test

Run these commands from the repository root. The solution targets `net10.0`; `global.json` selects .NET SDK `10.0.100` with `latestFeature` roll-forward.

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln
dotnet test G3AspNet.sln
```

Run one test by fully qualified name:

```bash
dotnet test tests/G3AspNet.Tests/G3AspNet.Tests.csproj --filter "FullyQualifiedName~G3AspNet.Tests.ImageUploadResultTests.Failure_DoesNotReportASuccessfulUpload"
```

GitHub Actions restores the solution, builds Release without restoring again, then runs tests without rebuilding:

```bash
dotnet restore G3AspNet.sln
dotnet build G3AspNet.sln --configuration Release --no-restore
dotnet test G3AspNet.sln --configuration Release --no-build --verbosity normal
```

Run the web app locally with `dotnet run --project G3AspNet.csproj --launch-profile http`. Restore the pinned EF Core CLI tool before using `dotnet ef`:

```bash
dotnet tool restore
dotnet ef migrations add DescriptiveName
dotnet ef database update
```

## Architecture

- This is a single ASP.NET Core MVC web project (`G3AspNet.csproj`) plus an xUnit project under `tests/G3AspNet.Tests`. `Program.cs` configures dependency injection, Identity, SQLite, MVC, cookie authentication, static files, routing, and the global antiforgery filter.
- HTTP endpoints live in `Controllers`; Razor pages are under the matching `Views` folders. Controllers handle request validation and HTTP responses, while account/profile/admin user operations are grouped in `Services/UserService`. Request and page data is carried by feature-specific `ViewModels`.
- `ApplicationDbContext` derives from `IdentityDbContext<ApplicationUser>`. `DbInitializer.InitializeAsync` applies EF migrations on startup, then idempotently seeds the `Admin` and `User` roles and demo accounts unless `SeedData:Enabled` is disabled. The SQLite connection and image-upload settings come from `appsettings*.json`.
- `ImageStorage` writes validated image uploads to `wwwroot/uploads/avatars`; the database stores only the generated filename. Extend `ImageFolder` and the related storage/view paths consistently when adding another image category.
- Domain model files exist under `Models`, but `ApplicationDbContext` currently configures only the Identity user model and does not expose domain `DbSet`s. Before treating a domain class as persisted, wire it into the context and add an EF migration.

## Repository conventions

- Keep namespaces aligned with folders using the `G3AspNet.*` root namespace. C# uses nullable reference types, implicit usings, file-scoped namespaces, and four-space indentation as configured in `.editorconfig`.
- Keep controller actions thin: use feature-specific view models, check `ModelState.IsValid` on form posts, delegate reusable account/user logic to services, and return the matching view or a redirect/appropriate HTTP result. Use `[HttpGet]`/`[HttpPost]` attributes and `[Authorize]`/role restrictions for access control. POST requests are protected globally by `AutoValidateAntiforgeryTokenAttribute`.
- Use `RoleNames` instead of repeating role string literals. For the authenticated user's ID, use `ClaimsPrincipalExtensions.GetRequiredUserId()` rather than parsing claims in each controller.
- Keep EF schema changes in `Data/Migrations` and generate them with the repository-pinned `dotnet-ef` tool (`.config/dotnet-tools.json`). Coordinate migrations with the team: do not hand-edit or delete migrations already on `main`; if a migration conflict arises, follow the recovery procedure in `README.md`.
- Preserve the separation between app and test projects: the web project excludes `tests/**` from its default items, and tests reference the web project from `tests/G3AspNet.Tests`.
