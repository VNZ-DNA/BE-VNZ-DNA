# Repository Guidelines

## Project Structure & Module Organization

This is a .NET 8 solution (`VNZ.sln`) organized as a layered backend:

- `VNZ.Api/` contains the ASP.NET Core entry point, controllers, middleware, DI extensions, static files, and runtime configuration.
- `VNZ.Service/` contains application services, request/response DTOs, validation-oriented business logic, authentication, mail, and media utilities. Feature folders commonly contain `IService.cs`, `Service.cs`, `Request.cs`, and `Response.cs`.
- `VNZ.Repository/` contains EF Core's `AppDbContext`, entity definitions, enums, JSON-owned types, and migrations.
- `VNZ.Test/` contains xUnit tests grouped by feature, such as `Products/`, `TeamMembers/`, and `JobApplications/`.

Keep dependency flow `Api -> Service -> Repository`; avoid placing HTTP concerns in services or business logic in controllers.

## Build, Test, and Development Commands

Run commands from the repository root:

```powershell
dotnet restore VNZ.sln              # restore NuGet packages
dotnet build VNZ.sln                # compile all projects
dotnet test VNZ.sln                 # run the xUnit suite
dotnet run --project VNZ.Api        # start the API locally
dotnet test VNZ.Test --filter "FullyQualifiedName~Products"
```

The API applies EF Core migrations at startup. Use local configuration and a safe development database before running it.

## Coding Style & Naming Conventions

Use C# conventions already present in the codebase: four-space indentation, file-scoped namespaces, nullable reference types, `PascalCase` for public types/members, and `camelCase` for locals and parameters. Use `Async` suffixes for asynchronous methods, e.g. `CreateMemberAsync`. Keep DTOs in feature-local `Request.cs` and `Response.cs`; add service contracts before implementations. No repository-wide formatter or linter is configured, so match nearby code and keep changes narrowly scoped.

## Testing Guidelines

Write xUnit tests with `[Fact]` for a single case and `[Theory]`/`[InlineData]` for input variations. Name tests as `MethodName_ExpectedBehavior`, for example `CreateMemberAsync_CreatesWorkingUnpublishedMember`. Service tests use EF Core's in-memory provider and a unique database name; follow that pattern to keep tests isolated. Add or update tests for new business rules and run the relevant feature filter before the full suite.

## Commit & Pull Request Guidelines

Recent history uses concise conventional prefixes such as `feat(admin): ...` and `fix ...`; prefer `feat(scope): summary`, `fix: summary`, or similarly clear imperative messages. Keep commits focused. Pull requests should describe the behavior change, link the related issue when available, list verification performed, and include request/response examples or screenshots for API-visible changes.

## Security & Configuration

Never commit credentials, JWT keys, mail passwords, or cloud provider secrets. `appsettings.Local.json`, environment variables, and `.env` are ignored for local overrides; document any new required setting without adding its production value.
