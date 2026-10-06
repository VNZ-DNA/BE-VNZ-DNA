using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Service.Localization;
using RichTextService = VNZ.Service.Utils.RichTextService;

var connectionString = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal))
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    await Console.Error.WriteLineAsync(
        "Missing PostgreSQL connection string. Pass it as the first argument or set ConnectionStrings__DefaultConnection.");
    return 2;
}

try
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connectionString)
        .Options;

    await using var dbContext = new AppDbContext(options);
    var checker = new BilingualIntegrityChecker(
        dbContext,
        new RichTextService.Service());
    var result = await checker.CheckAsync();

    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
    {
        WriteIndented = true
    });

    await Console.Out.WriteLineAsync(json);

    if (result.InfrastructureError is not null)
    {
        await Console.Error.WriteLineAsync(result.InfrastructureError);
    }

    return result.ExitCode;
}
catch (Exception exception)
{
    await Console.Error.WriteLineAsync($"bilingual-integrity-check failed: {exception.Message}");
    return 2;
}
