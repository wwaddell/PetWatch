namespace PetSitterApp.Models;

public enum SyncState
{
    Synced,
    PendingCreate,
    PendingUpdate,
    PendingDelete
}

public abstract class SyncEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public SyncState SyncState { get; set; } = SyncState.PendingCreate;
}
