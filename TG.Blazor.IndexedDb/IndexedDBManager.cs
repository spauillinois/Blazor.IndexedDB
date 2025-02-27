using Microsoft.JSInterop;

namespace TG.Blazor.IndexedDB;

public interface IIndexedDBManager
{
    int CurrentVersion { get; }
    string? DbName { get; }
    List<StoreSchema> Stores { get; }
    event EventHandler<IndexedDBNotificationArgs>? ActionCompleted;
    Task AddNewStore(StoreSchema storeSchema);
    Task AddRecord<T>(StoreRecord<T> recordToAdd);
    void CalledFromJS(string message);
    Task ClearStore(string storeName);
    Task DeleteDb(string dbName);
    Task DeleteRecord<TInput>(string storeName, TInput id);
    Task<IList<TResult>?> GetAllRecordsByIndex<TInput, TResult>(StoreIndexQuery<TInput> searchQuery);
    Task GetCurrentDbState();
    Task<TResult?> GetRecordById<TInput, TResult>(string storeName, TInput id);
    Task<TResult?> GetRecordByIndex<TInput, TResult>(StoreIndexQuery<TInput> searchQuery);
    Task<List<TResult>?> GetRecords<TResult>(string storeName);
    Task OpenDb();
    Task UpdateRecord<T>(StoreRecord<T> recordToUpdate);
}

/// <summary>
/// Provides functionality for accessing IndexedDB from Blazor application
/// </summary>
public sealed class IndexedDBManager(DbStore dbStore, IJSRuntime jsRuntime) : IIndexedDBManager
{
    private const string _interopPrefix = "TimeGhost.IndexedDbManager";
    private const string _javascriptPath = "./indexedDbBlazor.js"; // "./_content/TG.Blazor.IndexedDB/indexedDbBlazor.js";
    private IJSObjectReference? _module;
    private bool _isOpen;

    /// <summary>
    /// A notification event that is raised when an action is completed
    /// </summary>
    public event EventHandler<IndexedDBNotificationArgs>? ActionCompleted;
    public List<StoreSchema> Stores => dbStore.Stores;
    public int CurrentVersion => dbStore.Version;
    public string? DbName => dbStore.DbName;

    /// <summary>
    /// Opens the IndexedDB defined in the DbStore. Under the covers will create the database if it does not exist
    /// and create the stores defined in DbStore.
    /// </summary>
    /// <returns></returns>
    public async Task OpenDb()
    {
        var result = await CallJavascript<string>(DbFunctions.OpenDb, dbStore, new { Instance = DotNetObjectReference.Create(this), MethodName= "Callback"});
        _isOpen = true;

        await GetCurrentDbState();

        RaiseNotification(IndexDBActionOutCome.Successful, result);
    }

    /// <summary>
    /// Deletes the database corresponding to the dbName passed in
    /// </summary>
    /// <param name="dbName">The name of database to delete</param>
    /// <returns></returns>
    public async Task DeleteDb(string dbName)
    {
        if (string.IsNullOrEmpty(dbName))
        {
            throw new ArgumentException("dbName cannot be null or empty", nameof(dbName));
        }

        var result = await CallJavascript<string>(DbFunctions.DeleteDb, dbName);

        RaiseNotification(IndexDBActionOutCome.Successful, result);
    }

    public async Task GetCurrentDbState()
    {
        await EnsureDbOpen();

        var result = await CallJavascript<DbInformation>(DbFunctions.GetDbInfo, dbStore.DbName!);

        if (result.Version > dbStore.Version)
        {
            dbStore.Version = result.Version;

            var currentStores = dbStore.Stores.Select(s => s.Name);

            foreach (var storeName in result.StoreNames)
            {
                if (!currentStores.Contains(storeName))
                {
                    dbStore.Stores.Add(new StoreSchema(result.Version, storeName, null));

                }
            }
        }
    }

    /// <summary>
    /// This function provides the means to add a store to an existing database,
    /// </summary>
    /// <param name="storeSchema"></param>
    /// <returns></returns>
    public async Task AddNewStore(StoreSchema storeSchema)
    {
        if (storeSchema == null)
        {
            return;
        }

        if (dbStore.Stores.Any(s => s.Name == storeSchema.Name))
        {
            return;
        }

        dbStore.Stores.Add(storeSchema);
        dbStore.Version += 1;

        var result = await CallJavascript<string>(DbFunctions.OpenDb, dbStore, new { Instance = DotNetObjectReference.Create(this), MethodName = "Callback" });
        _isOpen = true;

        RaiseNotification(IndexDBActionOutCome.Successful, $"new store {storeSchema.Name} added");
    }

    /// <summary>
    /// Adds a new record/object to the specified store
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recordToAdd">An instance of StoreRecord that provides the store name and the data to add</param>
    /// <returns></returns>
    public async Task AddRecord<T>(StoreRecord<T> recordToAdd)
    {
        await EnsureDbOpen();

        try
        {
            var result = await CallJavascript<StoreRecord<T>, string>(DbFunctions.AddRecord, recordToAdd);
            RaiseNotification(IndexDBActionOutCome.Successful, result);
        }
        catch (JSException e)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, e.Message);
        }
    }

    /// <summary>
    /// Updates and existing record
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recordToUpdate">An instance of StoreRecord with the store name and the record to update</param>
    /// <returns></returns>
    public async Task UpdateRecord<T>(StoreRecord<T> recordToUpdate)
    {
        await EnsureDbOpen();

        try
        {
            var result = await CallJavascript<StoreRecord<T>, string>(DbFunctions.UpdateRecord, recordToUpdate);
            RaiseNotification(IndexDBActionOutCome.Successful, result);
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);
        }
    }

    /// <summary>
    /// Gets all of the records in a given store.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="storeName">The name of the store from which to retrieve the records</param>
    /// <returns></returns>
    public async Task<List<TResult>?> GetRecords<TResult>(string storeName)
    {
        await EnsureDbOpen();

        try
        {
            var results = await CallJavascript<List<TResult>>(DbFunctions.GetRecords, storeName);

            RaiseNotification(IndexDBActionOutCome.Successful, $"Retrieved {results.Count} records from {storeName}");

            return results;
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);

            return default;
        }
    }

    /// <summary>
    /// Retrieve a record by id
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="storeName">The name of the  store to retrieve the record from</param>
    /// <param name="id">the id of the record</param>
    /// <returns></returns>
    public async Task<TResult?> GetRecordById<TInput, TResult>(string storeName, TInput id)
    {
        await EnsureDbOpen();

        try
        {
            if (id == null)
            {
                RaiseNotification(IndexDBActionOutCome.Failed, "id cannot be null");

                return default;
            }

            var record = await CallJavascript<TResult>(DbFunctions.GetRecordById, storeName, id);

            return record;
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);

            return default;
        }
    }

    /// <summary>
    /// Deletes a record from the store based on the id
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <param name="storeName"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteRecord<TInput>(string storeName, TInput id)
    {
        try
        {
            if (id == null)
            {
                RaiseNotification(IndexDBActionOutCome.Failed, "id cannot be null");
                return;
            }

            await CallJavascript<string>(DbFunctions.DeleteRecord, storeName, id);
            RaiseNotification(IndexDBActionOutCome.Deleted, $"Deleted from {storeName} record: {id}");
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);
        }
    }

    /// <summary>
    /// Clears all of the records from a given store.
    /// </summary>
    /// <param name="storeName">The name of the store to clear the records from</param>
    /// <returns></returns>
    public async Task ClearStore(string storeName)
    {
        if (string.IsNullOrEmpty(storeName))
        {
            throw new ArgumentException("Parameter cannot be null or empty", nameof(storeName));
        }

        try
        {
            var result =  await CallJavascript<string, string>(DbFunctions.ClearStore, storeName);
            RaiseNotification(IndexDBActionOutCome.Successful, result);
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);
        }
    }

    /// <summary>
    /// Returns the first record that matches a query against a given index
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="searchQuery">an instance of StoreIndexQuery</param>
    /// <returns></returns>
    public async Task<TResult?> GetRecordByIndex<TInput, TResult>(StoreIndexQuery<TInput> searchQuery)
    {
        await EnsureDbOpen();

        try
        {
            var result = await CallJavascript<StoreIndexQuery<TInput>, TResult>(DbFunctions.GetRecordByIndex, searchQuery);
            return result;
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);
            return default;
        }
    }

    /// <summary>
    /// Gets all of the records that match a given query in the specified index.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="searchQuery"></param>
    /// <returns></returns>
    public async Task<IList<TResult>?> GetAllRecordsByIndex<TInput, TResult>(StoreIndexQuery<TInput> searchQuery)
    {
        await EnsureDbOpen();

        try
        {
            var results = await CallJavascript<StoreIndexQuery<TInput>, IList<TResult>>(DbFunctions.GetAllRecordsByIndex, searchQuery);
            RaiseNotification(IndexDBActionOutCome.Successful,
                $"Retrieved {results.Count} records, for {searchQuery.QueryValue} on index {searchQuery.IndexName}");
            return results;
        }
        catch (JSException jse)
        {
            RaiseNotification(IndexDBActionOutCome.Failed, jse.Message);
            return default;
        }
    }

    [JSInvokable("Callback")]
    public void CalledFromJS(string message) => Console.WriteLine($"called from JS: {message}");

    private async Task<TResult> CallJavascript<TData, TResult>(string functionName, TData data)
    {
        await InitializeModule();

        return await _module!.InvokeAsync<TResult>($"{_interopPrefix}.{functionName}", data);
    }

    private async Task<TResult> CallJavascript<TResult>(string functionName, params object[] args)
    {
        await InitializeModule();

        return await _module!.InvokeAsync<TResult>($"{_interopPrefix}.{functionName}", args);
    }

    private async Task InitializeModule()
    {
        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", _javascriptPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task EnsureDbOpen()
    {
        if (!_isOpen)
        {
            await OpenDb();
        }
    }

    private void RaiseNotification(IndexDBActionOutCome outcome, string message) => ActionCompleted?.Invoke(this, new IndexedDBNotificationArgs { Outcome = outcome, Message = message });
}
