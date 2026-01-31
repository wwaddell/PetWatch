using PetSitterApp.Models;

namespace PetSitterApp.Services;

public class SyncService
{
    private readonly LocalDbService _localDb;
    private readonly GoogleService _googleService;

    public SyncService(LocalDbService localDb, GoogleService googleService)
    {
        _localDb = localDb;
        _googleService = googleService;
    }

    public async Task SyncData()
    {
        await _googleService.EnsureSheetExists();

        // 1. Export Customers
        var customers = await _localDb.GetCustomers();
        var customerData = new List<IList<object>>
        {
            new List<object> { "Id", "FirstName", "LastName", "Email", "Phone", "Address", "IsDeleted" } // Header
        };
        foreach (var c in customers)
        {
            customerData.Add(new List<object> { c.Id.ToString(), c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.Address, c.IsDeleted });
        }
        await _googleService.PushData("Customers!A1", customerData);

        // 2. Export Pets
        var pets = await _localDb.GetPets();
        var petData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Name", "Species", "Breed", "Notes" }
        };
        foreach (var p in pets)
        {
            petData.Add(new List<object> { p.Id.ToString(), p.CustomerId.ToString(), p.Name, p.Species, p.Breed, p.Notes });
        }
        await _googleService.PushData("Pets!A1", petData);

        // 3. Export Appointments
        var appointments = await _localDb.GetAppointments();
        var apptData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Title", "Start", "End", "Description" }
        };
        foreach (var a in appointments)
        {
            apptData.Add(new List<object> { a.Id.ToString(), a.CustomerId.ToString(), a.Title, a.Start.ToString("o"), a.End.ToString("o"), a.Description });
        }
        await _googleService.PushData("Appointments!A1", apptData);

        // 4. Export Payments
        var payments = await _localDb.GetPayments();
        var paymentData = new List<IList<object>>
        {
            new List<object> { "Id", "AppointmentId", "Amount", "Method", "Date", "Notes" }
        };
        foreach (var p in payments)
        {
            paymentData.Add(new List<object> { p.Id.ToString(), p.AppointmentId.ToString(), p.Amount, p.Method, p.PaymentDate.ToString("o"), p.Notes });
        }
        await _googleService.PushData("Payments!A1", paymentData);

        // 5. Sync Calendar
        foreach (var appt in appointments)
        {
            if (appt.SyncState == SyncState.PendingCreate || appt.SyncState == SyncState.PendingUpdate)
            {
                await _googleService.SyncToCalendar(appt);
                appt.SyncState = SyncState.Synced;
                await _localDb.SaveAppointment(appt);
            }
        }
    }
}
