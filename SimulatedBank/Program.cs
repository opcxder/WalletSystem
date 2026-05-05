using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;
using SimulatedBank.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BankDbConnection")));

builder.Services.AddScoped<BankService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run(); 