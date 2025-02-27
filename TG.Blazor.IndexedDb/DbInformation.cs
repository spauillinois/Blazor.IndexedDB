namespace TG.Blazor.IndexedDB;

public sealed record DbInformation(int Version)
{
    public string[] StoreNames { get; set; } = [];
}
