using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using VNZ.Api.Extensions;
using VNZ.Api.Middleware;
using VNZ.Repository;

Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);

var builder = WebApplication.CreateBuilder(args);

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

if (app.Environment.IsDevelopment())
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
