namespace PetSitterApp.Models;

public class Customer : SyncEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public string FullName => $"{LastName}, {FirstName}";

    public Customer Clone()
    {
        return (Customer)this.MemberwiseClone();
    }

    public void CopyFrom(Customer other)
    {
        this.Id = other.Id;
        this.CreatedAt = other.CreatedAt;
        this.UpdatedAt = other.UpdatedAt;
        this.IsDeleted = other.IsDeleted;
        this.SyncState = other.SyncState;

        this.FirstName = other.FirstName;
        this.LastName = other.LastName;
        this.Email = other.Email;
        this.PhoneNumber = other.PhoneNumber;
        this.Address = other.Address;
        this.Notes = other.Notes;
    }
}
