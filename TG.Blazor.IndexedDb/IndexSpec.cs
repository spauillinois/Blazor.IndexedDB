namespace TG.Blazor.IndexedDB;

/// <summary>
/// Defines an Index for a given object store.
/// </summary>
/// <param name="Name"> The name of the index. </param>
/// <param name="KeyPath"> the identifier for the property in the object/record that is saved and is to be indexed. </param>
/// <param name="Unique"> defines whether the key value must be unique </param>
/// <param name="Auto"> determines whether the index value should be generate by IndexDB.
/// Only use if you are defining a primary key such as "id" </param>
public sealed record IndexSpec(string? Name, string? KeyPath, bool? Unique, bool Auto)
{
    public IndexSpec(bool auto) : this(null, null, null, auto)
    {
    }

    public IndexSpec(string name, string keyPath) : this(name, keyPath, null, true)
    {
    }

    public IndexSpec(string? name, string? keyPath, bool auto) : this(name, keyPath, null, auto)
    {
    }
}
