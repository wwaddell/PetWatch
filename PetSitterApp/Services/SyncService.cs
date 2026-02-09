using PetSitterApp.Models;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace PetSitterApp.Services;

public class SyncService
{
    private readonly LocalDbService _localDb;
    private readonly GoogleService _googleService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;

    public bool IsSyncing { get; private set; }
    public event Action? OnChange;

    public SyncService(LocalDbService localDb, GoogleService googleService, AuthenticationStateProvider authStateProvider, NavigationManager navigationManager)
    {
        _localDb = localDb;
        _googleService = googleService;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
    }

    public async Task TryAutoSync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                await SyncData();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auto-sync failed: {ex.Message}");
        }
    }

    public async Task SyncData()
    {
        if (IsSyncing) return;

        IsSyncing = true;
        OnChange?.Invoke();

        try
        {
            await _googleService.EnsureSheetExists();

            // 0. Fetch All Data
            var customers = await _localDb.GetCustomers();
            var pets = await _localDb.GetPets();
            var services = await _localDb.GetServices();
            var appointments = await _localDb.GetAppointments();
            var payments = await _localDb.GetPayments();

            // 1. Import Data (Pull & Merge)
            await ImportData(customers, pets, services, appointments, payments);

            // 2. Sync Calendar (Appointments)
            var customerDict = customers.ToDictionary(c => c.Id);
            var petDict = pets.ToDictionary(p => p.Id);

            var appointmentsToSave = new List<Appointment>();

            try
            {
                foreach (var appt in appointments)
                {
                    if (appt.SyncState == SyncState.PendingCreate || appt.SyncState == SyncState.PendingUpdate)
                    {
                        if (appt.IsDeleted)
                        {
                            if (!string.IsNullOrEmpty(appt.GoogleEventId))
                            {
                                await _googleService.DeleteEvent(appt.GoogleEventId);
                                appt.GoogleEventId = null;
                            }
                        }
                        else
                        {
                            string customerName = "Unknown Customer";
                            string customerAddress = "";
                            string customerNotes = "";
                            if (customerDict.ContainsKey(appt.CustomerId))
                            {
                                var c = customerDict[appt.CustomerId];
                                customerName = c.FullName;
                                customerAddress = c.Address;
                                customerNotes = c.Notes;
                            }

                            var petNames = new List<string>();
                            var petNotes = new List<string>();
                            foreach (var pid in appt.PetIds)
                            {
                                if (petDict.ContainsKey(pid))
                                {
                                    var p = petDict[pid];
                                    petNames.Add(p.Name);
                                    if (!string.IsNullOrWhiteSpace(p.Notes)) petNotes.Add($"{p.Name}: {p.Notes}");
                                }
                            }
                            string petNamesStr = string.Join(", ", petNames);
                            string petNotesStr = string.Join("; ", petNotes);

                            string title;
                            if (string.IsNullOrWhiteSpace(petNamesStr))
                            {
                                title = $"{customerName} - {appt.ServiceType}";
                            }
                            else
                            {
                                title = $"{customerName} - {petNamesStr} - {appt.ServiceType}";
                            }
                            string location = customerAddress;

                            var descBuilder = new System.Text.StringBuilder();
                            descBuilder.AppendLine($"Service: {appt.ServiceType}");
                            if (appt.VisitsPerDay > 0) descBuilder.AppendLine($"Visits Per Day: {appt.VisitsPerDay}");
                            descBuilder.AppendLine($"Expected: {appt.ExpectedAmount:C}");
                            if (!string.IsNullOrWhiteSpace(appt.Description)) descBuilder.AppendLine($"\nAppointment Notes:\n{appt.Description}");
                            if (!string.IsNullOrWhiteSpace(customerNotes)) descBuilder.AppendLine($"\nCustomer Notes:\n{customerNotes}");
                            if (!string.IsNullOrWhiteSpace(petNotesStr)) descBuilder.AppendLine($"\nPet Notes:\n{petNotesStr}");

                            await _googleService.SyncToCalendar(appt, title, location, descBuilder.ToString());
                        }

                        appt.SyncState = SyncState.Synced;
                        appointmentsToSave.Add(appt);
                    }
                }
            }
            finally
            {
                if (appointmentsToSave.Any())
                {
                    await _localDb.SaveAppointments(appointmentsToSave);
                }
            }

            // 3. Export Data (Push)
            await ExportData(customers, pets, appointments, payments, services);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Could not retrieve access token"))
            {
                _navigationManager.NavigateTo("authentication/login", forceLoad: true);
            }
            throw;
        }
        finally
        {
            IsSyncing = false;
            OnChange?.Invoke();
        }
    }

    private async Task ImportData(List<Customer> customers, List<Pet> pets, List<ServiceModel> services, List<Appointment> appointments, List<Payment> payments)
    {
        // Customers
        var remoteCustomerIds = await ImportSheet<Customer>("Customers!A2:I", async (row) =>
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
        }, customers, _localDb.SaveCustomers);
        await PruneDeletedRecords(customers, remoteCustomerIds, _localDb.DeleteCustomer);

        // Pets
        var remotePetIds = await ImportSheet<Pet>("Pets!A2:G", async (row) =>
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
        }, pets, _localDb.SavePets);
        await PruneDeletedRecords(pets, remotePetIds, _localDb.DeletePet);

        // Services
        var remoteServiceIds = await ImportSheet<ServiceModel>("Services!A2:F", async (row) =>
        {
            var s = new ServiceModel();
            s.Id = Guid.Parse(row[0].ToString());
            s.Name = row[1].ToString();
            s.DefaultRate = decimal.Parse(row[2].ToString());
            s.IsMultiplePerDay = bool.Parse(row[3].ToString());
            s.IsObsolete = bool.Parse(row[4].ToString());
            if (row.Count > 5) s.UpdatedAt = DateTime.Parse(row[5].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return s;
        }, services, _localDb.SaveServices);
        await PruneDeletedRecords(services, remoteServiceIds, _localDb.DeleteService);

        // Appointments
        var remoteApptIds = await ImportSheet<Appointment>("Appointments!A2:L", async (row) =>
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
            if (row.Count > 10) a.IsDeleted = bool.Parse(row[10].ToString());
            if (row.Count > 11) a.GoogleEventId = row[11].ToString();
            return a;
        }, appointments, _localDb.SaveAppointments);
        await PruneDeletedRecords(appointments, remoteApptIds, _localDb.DeleteAppointment);

        // Payments
        var remotePaymentIds = await ImportSheet<Payment>("Payments!A2:H", async (row) =>
        {
            var p = new Payment();
            p.Id = Guid.Parse(row[0].ToString());
            p.AppointmentId = Guid.Parse(row[1].ToString());
            p.Amount = decimal.Parse(row[2].ToString());
            p.Method = row[3].ToString();
            p.PaymentDate = DateTime.Parse(row[4].ToString());
            p.Notes = row[5].ToString();
            if (row.Count > 6) p.UpdatedAt = DateTime.Parse(row[6].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            if (row.Count > 7) p.IsDeleted = bool.Parse(row[7].ToString());
            return p;
        }, payments, _localDb.SavePayments);
        await PruneDeletedRecords(payments, remotePaymentIds, _localDb.DeletePayment);
    }

    private async Task PruneDeletedRecords<T>(List<T> localItems, HashSet<Guid> remoteIds, Func<Guid, Task> deleteLocalFunc) where T : SyncEntity
    {
        var itemsToDelete = localItems.Where(i => i.IsDeleted && !remoteIds.Contains(i.Id)).ToList();

        foreach (var item in itemsToDelete)
        {
            await deleteLocalFunc(item.Id);
            localItems.Remove(item);
        }
    }

    private async Task<HashSet<Guid>> ImportSheet<T>(string range, Func<IList<object>, Task<T>> mapper, List<T> localItems, Func<List<T>, Task> saveLocalBatch) where T : SyncEntity
    {
        var remoteIds = new HashSet<Guid>();
        var rows = await _googleService.ReadData(range);
        if (rows == null || rows.Count == 0) return remoteIds;

        var localDict = localItems.ToDictionary(i => i.Id);
        var itemsToSave = new List<T>();

        foreach (var row in rows)
        {
            try
            {
                var remoteItem = await mapper(row);
                remoteIds.Add(remoteItem.Id);

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
                    itemsToSave.Add(remoteItem);

                    // Update in-memory list
                    var index = localItems.FindIndex(x => x.Id == remoteItem.Id);
                    if (index >= 0)
                    {
                        localItems[index] = remoteItem;
                    }
                    else
                    {
                        localItems.Add(remoteItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing row: {ex.Message}");
            }
        }

        if (itemsToSave.Any())
        {
            await saveLocalBatch(itemsToSave);
        }

        return remoteIds;
    }

    private async Task ExportData(List<Customer> customers, List<Pet> pets, List<Appointment> appointments, List<Payment> payments, List<ServiceModel> services)
    {
        // 1. Export Customers
        var customerData = new List<IList<object>>
        {
            new List<object> { "Id", "FirstName", "LastName", "Email", "Phone", "Address", "IsDeleted", "UpdatedAt", "Notes" } // Header
        };
        foreach (var c in customers)
        {
            customerData.Add(new List<object> { c.Id.ToString(), c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.Address, c.IsDeleted, ToUtcString(c.UpdatedAt), c.Notes });
        }
        await _googleService.PushData("Customers!A1", customerData);
        var unsyncedCustomers = customers.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedCustomers.Any())
        {
            foreach (var c in unsyncedCustomers) c.SyncState = SyncState.Synced;
            await _localDb.SaveCustomers(unsyncedCustomers);
        }

        // 2. Export Pets
        var petData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Name", "Species", "Breed", "Notes", "UpdatedAt" }
        };
        foreach (var p in pets)
        {
            petData.Add(new List<object> { p.Id.ToString(), p.CustomerId.ToString(), p.Name, p.Species, p.Breed, p.Notes, ToUtcString(p.UpdatedAt) });
        }
        await _googleService.PushData("Pets!A1", petData);
        var unsyncedPets = pets.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedPets.Any())
        {
            foreach (var p in unsyncedPets) p.SyncState = SyncState.Synced;
            await _localDb.SavePets(unsyncedPets);
        }

        // 3. Export Appointments
        var apptData = new List<IList<object>>
        {
            new List<object> { "Id", "CustomerId", "Start", "End", "Description", "ServiceType", "Rate", "ExpectedAmount", "PetIds", "UpdatedAt", "IsDeleted", "GoogleEventId" }
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
                ToUtcString(a.UpdatedAt),
                a.IsDeleted,
                a.GoogleEventId ?? ""
            });
        }
        await _googleService.PushData("Appointments!A1", apptData);
        // We do NOT mark appointments as synced here, because Calendar Sync (which runs before ExportData)
        // is responsible for checking pending status and syncing to calendar.
        // If we mark them synced here, Calendar sync would skip them on next run if it failed previously.

        // 4. Export Payments
        var paymentData = new List<IList<object>>
        {
            new List<object> { "Id", "AppointmentId", "Amount", "Method", "Date", "Notes", "UpdatedAt", "IsDeleted" }
        };
        foreach (var p in payments)
        {
            paymentData.Add(new List<object> { p.Id.ToString(), p.AppointmentId.ToString(), p.Amount, p.Method, ToUtcString(p.PaymentDate), p.Notes, ToUtcString(p.UpdatedAt), p.IsDeleted });
        }
        await _googleService.PushData("Payments!A1", paymentData);
        var unsyncedPayments = payments.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedPayments.Any())
        {
            foreach (var p in unsyncedPayments) p.SyncState = SyncState.Synced;
            await _localDb.SavePayments(unsyncedPayments);
        }

        // 5. Export Services
        var serviceData = new List<IList<object>>
        {
            new List<object> { "Id", "Name", "DefaultRate", "IsMultiplePerDay", "IsObsolete", "UpdatedAt" }
        };
        foreach (var s in services)
        {
            serviceData.Add(new List<object> { s.Id.ToString(), s.Name, s.DefaultRate, s.IsMultiplePerDay, s.IsObsolete, ToUtcString(s.UpdatedAt) });
        }
        await _googleService.PushData("Services!A1", serviceData);
        var unsyncedServices = services.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedServices.Any())
        {
            foreach (var s in unsyncedServices) s.SyncState = SyncState.Synced;
            await _localDb.SaveServices(unsyncedServices);
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
