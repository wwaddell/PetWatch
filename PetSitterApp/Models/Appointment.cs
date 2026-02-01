namespace PetSitterApp.Models;

public class Appointment : SyncEntity
{
    public Guid CustomerId { get; set; }
    public List<Guid> PetIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string ServiceType { get; set; } = "Visit";
    public decimal Rate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public string? GoogleEventId { get; set; }
}
