using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<BankContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("BankDbConnection"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Run();