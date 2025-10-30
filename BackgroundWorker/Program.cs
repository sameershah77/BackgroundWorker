using BackgroundWorker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<AverageCalculationWorker>();
builder.Services.AddHostedService<CombinationInsertWorker>();



var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<DBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDbContext<DBContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


var host = builder.Build();
host.Run();
