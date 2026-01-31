namespace PetSitterApp.Models;

public class Appointment : SyncEntity
{
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? GoogleEventId { get; set; }
}
