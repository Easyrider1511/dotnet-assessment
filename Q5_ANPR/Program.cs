using Microsoft.EntityFrameworkCore;
using Q5_ANPR;
using Q5_ANPR.Data;
using Q5_ANPR.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AnprDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=anpr.db"));

builder.Services.AddScoped<PlateReadRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
