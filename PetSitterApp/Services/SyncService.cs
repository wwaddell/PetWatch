using PetSitterApp.Models;
using System.Globalization;

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

        // 1. Import Data (Pull & Merge)
        await ImportData();

        // 2. Export Data (Push)
        await ExportData();

        // 3. Sync Calendar (Appointments)
        var appointments = await _localDb.GetAppointments();
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

    private async Task ImportData()
    {
        // Customers
        await ImportSheet<Customer>("Customers!A2:I", async (row) =>
        {
             var c = new Customer();
             c.Id = Guid.Parse(row[0].ToString());
             c.FirstName = row[1].ToString();
             c.LastName = row[2].ToString();
             c.Email = row[3].ToString();
             c.PhoneNumber = row[4].ToString();
             c.Address = row[5].ToString();
             c.IsDeleted = bool.Parse(row[6].ToString());
             if(row.Count > 7) c.UpdatedAt = DateTime.Parse(row[7].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
             if(row.Count > 8) c.Notes = row[8].ToString();
             return c;
        }, _localDb.GetCustomers, _localDb.SaveCustomer);

        // Pets
        await ImportSheet<Pet>("Pets!A2:G", async (row) =>
        {
            var p = new Pet();
            p.Id = Guid.Parse(row[0].ToString());
            p.CustomerId = Guid.Parse(row[1].ToString());
            p.Name = row[2].ToString();
            p.Species = row[3].ToString();
            p.Breed = row[4].ToString();
            p.Notes = row[5].ToString();
            if (row.Count > 6) p.UpdatedAt = DateTime.Parse(row[6].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return p;
        }, _localDb.GetPets, _localDb.SavePet);

        // Services
        await ImportSheet<ServiceModel>("Services!A2:F", async (row) =>
        {
            var s = new ServiceModel();
            s.Id = Guid.Parse(row[0].ToString());
            s.Name = row[1].ToString();
            s.DefaultRate = decimal.Parse(row[2].ToString());
            s.IsMultiplePerDay = bool.Parse(row[3].ToString());
            s.IsObsolete = bool.Parse(row[4].ToString());
            if (row.Count > 5) s.UpdatedAt = DateTime.Parse(row[5].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return s;
        }, _localDb.GetServices, _localDb.SaveService);

        // Appointments
        await ImportSheet<Appointment>("Appointments!A2:J", async (row) =>
        {
            var a = new Appointment();
            a.Id = Guid.Parse(row[0].ToString());
            a.CustomerId = Guid.Parse(row[1].ToString());
            a.Start = string.IsNullOrEmpty(row[2].ToString()) ? null : DateTime.Parse(row[2].ToString());
            a.End = string.IsNullOrEmpty(row[3].ToString()) ? null : DateTime.Parse(row[3].ToString());
            a.Description = row[4].ToString();
            a.ServiceType = row[5].ToString();
            a.Rate = decimal.Parse(row[6].ToString());
            a.ExpectedAmount = decimal.Parse(row[7].ToString());

            var petIdsStr = row[8].ToString();
            if (!string.IsNullOrEmpty(petIdsStr))
            {
                a.PetIds = petIdsStr.Split(',').Select(Guid.Parse).ToList();
            }

            if (row.Count > 9) a.UpdatedAt = DateTime.Parse(row[9].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return a;
        }, _localDb.GetAppointments, _localDb.SaveAppointment);

        // Payments
        await ImportSheet<Payment>("Payments!A2:G", async (row) =>
        {
            var p = new Payment();
            p.Id = Guid.Parse(row[0].ToString());
            p.AppointmentId = Guid.Parse(row[1].ToString());
            p.Amount = decimal.Parse(row[2].ToString());
            p.Method = row[3].ToString();
            p.PaymentDate = DateTime.Parse(row[4].ToString());
            p.Notes = row[5].ToString();
            if (row.Count > 6) p.UpdatedAt = DateTime.Parse(row[6].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return p;
        }, _localDb.GetPayments, _localDb.SavePayment);
    }

    private async Task ImportSheet<T>(string range, Func<IList<object>, Task<T>> mapper, Func<Task<List<T>>> getLocal, Func<T, Task> saveLocal) where T : SyncEntity
    {
        var rows = await _googleService.ReadData(range);
        if (rows == null || rows.Count == 0) return;

        var localItems = await getLocal();
        var localDict = localItems.ToDictionary(i => i.Id);

        foreach (var row in rows)
        {
            try
            {
                var remoteItem = await mapper(row);

                bool shouldSave = false;
                if (!localDict.ContainsKey(remoteItem.Id))
                {
                    shouldSave = true; // New item from remote
                }
                else
                {
                    var localItem = localDict[remoteItem.Id];
                    if (remoteItem.UpdatedAt > localItem.UpdatedAt)
                    {
                        shouldSave = true; // Remote is newer
                    }
                }

                if (shouldSave)
                {
                    remoteItem.SyncState = SyncState.Synced; // It came from server, so it's synced
                    await saveLocal(remoteItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing row: {ex.Message}");
            }
        }
    }

    private async Task ExportData()
    {
        // 1. Export Customers
        var customers = await _localDb.GetCustomers();
        var customerData = new List<IList<object>>
        {
            new List<object> { "Id", "FirstName", "LastName", "Email", "Phone", "Address", "IsDeleted", "UpdatedAt", "Notes" } // Header
        };
        foreach (var c in customers)
        {
            customerData.Add(new List<object> { c.Id.ToString(), c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.Address, c.IsDeleted, ToUtcString(c.UpdatedAt), c.Notes });
        }
        await _googleService.PushData("Customers!A1", customerData);
        foreach (var c in customers.Where(x => x.SyncState != SyncState.Synced))
        {
            c.SyncState = SyncState.Synced;
            await _localDb.SaveCustomer(c);
        }

        // 2. Export Pets
        var pets = await _localDb.GetPets();
        var petData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Name", "Species", "Breed", "Notes", "UpdatedAt" }
        };
        foreach (var p in pets)
        {
            petData.Add(new List<object> { p.Id.ToString(), p.CustomerId.ToString(), p.Name, p.Species, p.Breed, p.Notes, ToUtcString(p.UpdatedAt) });
        }
        await _googleService.PushData("Pets!A1", petData);
        foreach (var p in pets.Where(x => x.SyncState != SyncState.Synced))
        {
            p.SyncState = SyncState.Synced;
            await _localDb.SavePet(p);
        }

        // 3. Export Appointments
        var appointments = await _localDb.GetAppointments();
        var apptData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Start", "End", "Description", "ServiceType", "Rate", "ExpectedAmount", "PetIds", "UpdatedAt" }
        };
        foreach (var a in appointments)
        {
            apptData.Add(new List<object> {
                a.Id.ToString(),
                a.CustomerId.ToString(),
                a.Start?.ToString("yyyy-MM-dd") ?? "",
                a.End?.ToString("yyyy-MM-dd") ?? "",
                a.Description,
                a.ServiceType,
                a.Rate,
                a.ExpectedAmount,
                string.Join(",", a.PetIds),
                ToUtcString(a.UpdatedAt)
            });
        }
        await _googleService.PushData("Appointments!A1", apptData);
        foreach (var a in appointments.Where(x => x.SyncState != SyncState.Synced))
        {
            a.SyncState = SyncState.Synced;
            await _localDb.SaveAppointment(a);
        }

        // 4. Export Payments
        var payments = await _localDb.GetPayments();
        var paymentData = new List<IList<object>>
        {
            new List<object> { "Id", "AppointmentId", "Amount", "Method", "Date", "Notes", "UpdatedAt" }
        };
        foreach (var p in payments)
        {
            paymentData.Add(new List<object> { p.Id.ToString(), p.AppointmentId.ToString(), p.Amount, p.Method, ToUtcString(p.PaymentDate), p.Notes, ToUtcString(p.UpdatedAt) });
        }
        await _googleService.PushData("Payments!A1", paymentData);
        foreach (var p in payments.Where(x => x.SyncState != SyncState.Synced))
        {
            p.SyncState = SyncState.Synced;
            await _localDb.SavePayment(p);
        }

        // 5. Export Services
        var services = await _localDb.GetServices();
        var serviceData = new List<IList<object>>
        {
            new List<object> { "Id", "Name", "DefaultRate", "IsMultiplePerDay", "IsObsolete", "UpdatedAt" }
        };
        foreach (var s in services)
        {
            serviceData.Add(new List<object> { s.Id.ToString(), s.Name, s.DefaultRate, s.IsMultiplePerDay, s.IsObsolete, ToUtcString(s.UpdatedAt) });
        }
        await _googleService.PushData("Services!A1", serviceData);
        foreach (var s in services.Where(x => x.SyncState != SyncState.Synced))
        {
            s.SyncState = SyncState.Synced;
            await _localDb.SaveService(s);
        }
    }

    private string ToUtcString(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss");
        }
        return dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }
}
