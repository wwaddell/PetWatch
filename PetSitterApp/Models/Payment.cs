namespace PetSitterApp.Models;

public class Payment : SyncEntity
{
    public Guid AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "Cash"; // Cash, Card, Transfer, etc.
    public string Notes { get; set; } = string.Empty;
}
