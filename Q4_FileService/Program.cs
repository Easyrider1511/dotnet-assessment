using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Q4_FileService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SQLite database for file metadata
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=fileservice.db"));

// Azure Blob Storage client — reads "AzureBlobStorage" from appsettings / environment
builder.Services.AddSingleton(new BlobServiceClient(
    builder.Configuration.GetConnectionString("AzureBlobStorage")
        ?? "UseDevelopmentStorage=true"));  // Azurite local emulator default

var app = builder.Build();

// Ensure the SQLite database is created on first run
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
