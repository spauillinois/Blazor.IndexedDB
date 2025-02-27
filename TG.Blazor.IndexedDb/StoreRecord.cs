namespace TG.Blazor.IndexedDB;

/// <summary>
/// This class is used when adding or updating a record.
/// </summary>
/// <param name="Storename"> The name of database store in each the record is to be saved </param>
/// <param name="Data"> The data/record to save in the store. </param>
/// <typeparam name="T"></typeparam>
public sealed record StoreRecord<T>(string? Storename, T? Data);
