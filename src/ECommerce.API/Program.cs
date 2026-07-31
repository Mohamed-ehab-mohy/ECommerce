using ECommerce.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi();

var app = builder.Build();

app.MapGet("/", () => "ECommerce API");

app.Run();
