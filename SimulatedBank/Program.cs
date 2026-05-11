using Microsoft.EntityFrameworkCore;
using SimulatedBank.Data;
using SimulatedBank.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BankDbConnection")));

builder.Services.AddScoped<BankService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddConsole();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.MapControllers();
app.Run(); 