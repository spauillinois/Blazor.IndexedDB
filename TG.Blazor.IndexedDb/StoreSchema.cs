namespace TG.Blazor.IndexedDB;

/// <summary>
/// Defines a store to add to database
/// </summary>
/// <param name="DbVersion"></param>
/// <param name="Name"> The name for the store </param>
/// <param name="PrimaryKey"> Defines the primary key to use. If not defined automatically creates a primary that is 
/// set to true for auto increment, and has the name and path of "id" </param>
public sealed record StoreSchema(int? DbVersion, string? Name, IndexSpec? PrimaryKey)
{
    /// <summary>
    /// Provides a set of additional indexes if required.
    /// </summary>
    public List<IndexSpec> Indexes { get; set; } = [];

    public StoreSchema(string? name, IndexSpec? primaryKey) : this(null, name, primaryKey)
    {
    }
}
