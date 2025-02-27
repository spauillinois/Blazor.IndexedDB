namespace TG.Blazor.IndexedDB;
public sealed class IndexedDBNotificationArgs : EventArgs
{
    public IndexDBActionOutCome Outcome { get; set; }
    public string? Message { get; set; }
}

public enum IndexDBActionOutCome
{
    Successful = 0,
    Failed = 1,
    Deleted = 2
}