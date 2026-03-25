namespace PetSitterApp.Models;

public class Appointment : SyncEntity
{
    public Guid CustomerId { get; set; }
    public Guid[] PetIds { get; set; } = Array.Empty<Guid>();
    public string Description { get; set; } = string.Empty;
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string ServiceType { get; set; } = "Visit";
    public int VisitsPerDay { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public string? GoogleEventId { get; set; }

    public Appointment Clone()
    {
        var clone = (Appointment)this.MemberwiseClone();
        clone.PetIds = (Guid[])this.PetIds.Clone();
        return clone;
    }

    public void CopyFrom(Appointment other)
    {
        this.Id = other.Id;
        this.CreatedAt = other.CreatedAt;
        this.UpdatedAt = other.UpdatedAt;
        this.IsDeleted = other.IsDeleted;
        this.SyncState = other.SyncState;

        this.CustomerId = other.CustomerId;
        this.PetIds = (Guid[])other.PetIds.Clone();
        this.Description = other.Description;
        this.Start = other.Start;
        this.End = other.End;
        this.ServiceType = other.ServiceType;
        this.VisitsPerDay = other.VisitsPerDay;
        this.Rate = other.Rate;
        this.ExpectedAmount = other.ExpectedAmount;
        this.GoogleEventId = other.GoogleEventId;
    }
}
