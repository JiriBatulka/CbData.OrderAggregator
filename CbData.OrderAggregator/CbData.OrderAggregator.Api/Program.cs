using CbData.OrderAggregator.Api;
using CbData.OrderAggregator.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOrderAggregator(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOrderEndpoints();
app.Run();