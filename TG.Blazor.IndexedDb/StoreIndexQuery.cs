namespace TG.Blazor.IndexedDB;

/// <summary>
/// Class used for running an index query.
/// </summary>
/// <param name="Storename"> The name of store that the index query will run against </param>
/// <param name="IndexName"> The name of the index to use for the query </param>
/// <param name="AllMatching"> By default IndexedDB will only return the first match in an index query.
/// Set this value to true if you want to return all the records that match the query </param>
/// <param name="QueryValue"> The value to search for </param>
/// <typeparam name="TInput"></typeparam>
public sealed record StoreIndexQuery<TInput>(string? Storename, string? IndexName, bool AllMatching, TInput? QueryValue)
{
    public StoreIndexQuery(string? storename, string? indexName, TInput? queryValue) : this(storename, indexName, false, queryValue)
    {
    }
}
