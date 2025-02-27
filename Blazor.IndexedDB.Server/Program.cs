using Blazor.IndexedDB.Server;
using Blazor.IndexedDB.Server.Data;

using TG.Blazor.IndexedDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddIndexedDB(dbStore =>
{
    dbStore.DbName = "TheFactory";
    dbStore.Version = 1;

    dbStore.Stores.Add(new StoreSchema(dbStore.Version, "Employees", new("id", "id"))
    {
        Indexes =
        [
            new("firstName","firstName",false),
                    new("lastName", "lastName",false)
        ]
    });

    dbStore.Stores.Add(new StoreSchema(dbStore.Version, "Outbox", new(true)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
