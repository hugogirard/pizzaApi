using Contoso.Models;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDaprClient();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => 
{
    o.RoutePrefix = string.Empty;
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");    
});

app.UseHttpsRedirection();

app.MapGet("/pizzas", async (DaprClient daprClient) =>
{
    var pizzas = await daprClient.GetStateAsync<Pizza[]>("pizzastatestore", "pizzas");
    return pizzas;
});

app.MapGet("/pizzas/{id}", async (DaprClient daprClient, int id) =>
{
    var pizza = await daprClient.GetStateAsync<Pizza>("pizzastatestore", $"pizzas/{id}");
    return pizza;
});

app.MapPost("/pizzas", async (DaprClient daprClient, Pizza pizza) =>
{
    await daprClient.SaveStateAsync("pizzastatestore", $"pizzas/{pizza.Id}", pizza);
    var pizzas = await daprClient.GetStateAsync<List<Pizza>>("pizzastatestore", "pizzas");
    if (pizzas is null)
    {
        pizzas = new List<Pizza>();
    }
    pizzas.Add(pizza);
    await daprClient.SaveStateAsync("pizzastatestore", "pizzas", pizzas);    
    return pizza;
});

app.MapDelete("/pizzas/{id}", async (DaprClient daprClient, int id) =>
{
    await daprClient.DeleteStateAsync("pizzastatestore", $"pizzas/{id}");
    var pizzas = await daprClient.GetStateAsync<List<Pizza>>("pizzastatestore", "pizzas");
    if (pizzas is not null)
    {
        var pizzaToRemove = pizzas.FirstOrDefault(p => p.Id == id);
        if (pizzaToRemove is not null)
        {
            pizzas.Remove(pizzaToRemove);
            await daprClient.SaveStateAsync("pizzastatestore", "pizzas", pizzas);
        }
    }
    return Results.NoContent();
});

app.Run();
