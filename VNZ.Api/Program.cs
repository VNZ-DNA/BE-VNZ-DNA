using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using VNZ.Api.Extensions;
using VNZ.Api.Middleware;
using VNZ.Repository;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Keep Data Protection keys in a writable directory inside the container.
// Mount this directory as persistent storage in production to retain keys
// across container replacements and restarts.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddBaseServices(builder.Configuration);

// TODO: Khi chốt từng module, đăng ký service ở đây hoặc tách thành extension riêng.
// Ví dụ:
// builder.Services.AddScoped<NewsService.IService, NewsService.Service>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Swagger is normally limited to Development. Enable it in a deployment by
// setting Swagger:Enabled=true (for example, Swagger__Enabled=true in Docker).
var swaggerEnabled = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
