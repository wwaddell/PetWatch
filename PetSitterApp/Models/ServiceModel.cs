namespace PetSitterApp.Models;

public class ServiceModel : SyncEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; }
    public bool IsMultiplePerDay { get; set; }
    public bool IsObsolete { get; set; }
}
