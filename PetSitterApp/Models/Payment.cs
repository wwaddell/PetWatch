namespace PetSitterApp.Models;

public class Payment : SyncEntity
{
    public Guid AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "Cash"; // Cash, Card, Transfer, etc.
    public string Notes { get; set; } = string.Empty;

    public Payment Clone()
    {
        return (Payment)this.MemberwiseClone();
    }

    public void CopyFrom(Payment other)
    {
        this.Id = other.Id;
        this.CreatedAt = other.CreatedAt;
        this.UpdatedAt = other.UpdatedAt;
        this.IsDeleted = other.IsDeleted;
        this.SyncState = other.SyncState;

        this.AppointmentId = other.AppointmentId;
        this.Amount = other.Amount;
        this.PaymentDate = other.PaymentDate;
        this.Method = other.Method;
        this.Notes = other.Notes;
    }
}
