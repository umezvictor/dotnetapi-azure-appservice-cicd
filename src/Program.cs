using Azure.Storage.Blobs;
using ImageResizerAPI;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();


builder.Services.AddSignalR();

builder.Services.AddSingleton(x =>
{
    string connectionString = configuration.GetSection("AzStorage")["Conn"]!;
    return new BlobServiceClient(connectionString);
});

builder.Host.UseSerilog();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance =
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

        Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});


var connection = configuration.GetConnectionString("SQLCONNSTR_CONNECTIONSTRING");

builder.Services.AddDbContext<ProcessedFileDbContext>(options =>
    options.UseSqlServer(connection));


var app = builder.Build();

// Apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ProcessedFileDbContext>();
        db.Database.Migrate();
        //Log.Information("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.MapControllers();
app.Run();
