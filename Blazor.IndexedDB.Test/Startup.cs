using Microsoft.Extensions.DependencyInjection;

using TG.Blazor.IndexedDB;

namespace Blazor.IndexedDB.Test;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIndexedDB(dbStore =>
        {
            dbStore.DbName = "TheFactory";
            dbStore.Version = 1;

            dbStore.Stores.Add(new StoreSchema(dbStore.Version, "Employees", new("id", "id", true))
            {
                Indexes =
                [
                    new IndexSpec("firstName", "firstName", false),
                    new IndexSpec("lastName", "lastName", false)
                ]
            });

            dbStore.Stores.Add(new StoreSchema(dbStore.Version, "Outbox", new(true)));
        });
    }

    //public void Configure(IComponentsApplicationBuilder app) => app.AddComponent<App>("app");
}
