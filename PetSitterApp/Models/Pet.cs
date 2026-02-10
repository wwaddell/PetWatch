namespace PetSitterApp.Models;

public class Pet : SyncEntity
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Pet Clone()
    {
        return (Pet)this.MemberwiseClone();
    }

    public void CopyFrom(Pet other)
    {
        this.Id = other.Id;
        this.CreatedAt = other.CreatedAt;
        this.UpdatedAt = other.UpdatedAt;
        this.IsDeleted = other.IsDeleted;
        this.SyncState = other.SyncState;

        this.CustomerId = other.CustomerId;
        this.Name = other.Name;
        this.Species = other.Species;
        this.Breed = other.Breed;
        this.DateOfBirth = other.DateOfBirth;
        this.DateOfDeath = other.DateOfDeath;
        this.Notes = other.Notes;
    }
}
