namespace PetSitterApp.Models;

public class Pet : SyncEntity
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Notes { get; set; } = string.Empty;
}
