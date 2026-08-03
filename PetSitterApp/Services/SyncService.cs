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

    private readonly List<string> _warnings = new();

    /// <summary>
    /// Non-fatal problems from the most recent sync - typically rows that could
    /// not be read. A sync can report success while quietly dropping rows, so
    /// these are surfaced on the Settings page instead of the console alone.
    /// </summary>
    public IReadOnlyList<string> LastSyncWarnings => _warnings;

    private void RecordWarning(string message)
    {
        _warnings.Add(message);
        Console.WriteLine($"Sync warning: {message}");
    }

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
        _warnings.Clear();
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
        catch (AccessTokenUnavailableException)
        {
            _navigationManager.NavigateTo("authentication/login", forceLoad: true);
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
        var remoteCustomerIds = await ImportSheet(SheetSchema.CustomersRange, SheetSchema.ToCustomer, customers, _localDb.SaveCustomers);
        await PruneDeletedRecords(customers, remoteCustomerIds, _localDb.DeleteCustomer);

        var remotePetIds = await ImportSheet(SheetSchema.PetsRange, SheetSchema.ToPet, pets, _localDb.SavePets);
        await PruneDeletedRecords(pets, remotePetIds, _localDb.DeletePet);

        var remoteServiceIds = await ImportSheet(SheetSchema.ServicesRange, SheetSchema.ToService, services, _localDb.SaveServices);
        await PruneDeletedRecords(services, remoteServiceIds, _localDb.DeleteService);

        var remoteApptIds = await ImportSheet(SheetSchema.AppointmentsRange, SheetSchema.ToAppointment, appointments, _localDb.SaveAppointments);
        await PruneDeletedRecords(appointments, remoteApptIds, _localDb.DeleteAppointment);

        var remotePaymentIds = await ImportSheet(SheetSchema.PaymentsRange, SheetSchema.ToPayment, payments, _localDb.SavePayments);
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

    private async Task<HashSet<Guid>> ImportSheet<T>(string range, Func<IList<object>, T> mapper, List<T> localItems, Func<List<T>, Task> saveLocalBatch) where T : SyncEntity
    {
        var remoteIds = new HashSet<Guid>();
        var rows = await _googleService.ReadData(range);
        if (rows == null || rows.Count == 0) return remoteIds;

        // Optimization: Create an index map to avoid O(N*M) lookups
        var localIndexMap = new Dictionary<Guid, int>(localItems.Count);
        for (int i = 0; i < localItems.Count; i++)
        {
            localIndexMap.TryAdd(localItems[i].Id, i);
        }

        var itemsToSave = new List<T>();

        foreach (var row in rows)
        {
            try
            {
                var remoteItem = mapper(row);
                remoteIds.Add(remoteItem.Id);

                bool shouldSave = false;
                int existingIndex = -1;

                if (localIndexMap.TryGetValue(remoteItem.Id, out int idx))
                {
                    existingIndex = idx;
                    var localItem = localItems[existingIndex];
                    if (remoteItem.UpdatedAt > localItem.UpdatedAt)
                    {
                        shouldSave = true; // Remote is newer
                    }
                }
                else
                {
                    shouldSave = true; // New item from remote
                }

                if (shouldSave)
                {
                    remoteItem.SyncState = SyncState.Synced; // It came from server, so it's synced
                    itemsToSave.Add(remoteItem);

                    // Update in-memory list
                    if (existingIndex >= 0)
                    {
                        localItems[existingIndex] = remoteItem;
                    }
                    else
                    {
                        localItems.Add(remoteItem);
                        // Update index map for the newly added item
                        localIndexMap[remoteItem.Id] = localItems.Count - 1;
                    }
                }
            }
            catch (Exception ex)
            {
                // A skipped row means data silently absent from the app, so it
                // is reported rather than only written to the browser console.
                var sheet = range.Split('!')[0];
                RecordWarning($"{sheet}: skipped a row - {ex.Message}");
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
        var customerData = new List<IList<object>> { SheetSchema.CustomerHeader() };
        foreach (var c in customers)
        {
            customerData.Add(SheetSchema.ToRow(c));
        }
        await _googleService.PushData("Customers!A1", customerData);
        var unsyncedCustomers = customers.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedCustomers.Any())
        {
            foreach (var c in unsyncedCustomers) c.SyncState = SyncState.Synced;
            await _localDb.SaveCustomers(unsyncedCustomers);
        }

        // 2. Export Pets
        var petData = new List<IList<object>> { SheetSchema.PetHeader() };
        foreach (var p in pets)
        {
            petData.Add(SheetSchema.ToRow(p));
        }
        await _googleService.PushData("Pets!A1", petData);
        var unsyncedPets = pets.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedPets.Any())
        {
            foreach (var p in unsyncedPets) p.SyncState = SyncState.Synced;
            await _localDb.SavePets(unsyncedPets);
        }

        // 3. Export Appointments
        var apptData = new List<IList<object>> { SheetSchema.AppointmentHeader() };
        foreach (var a in appointments)
        {
            apptData.Add(SheetSchema.ToRow(a));
        }
        await _googleService.PushData("Appointments!A1", apptData);
        // We do NOT mark appointments as synced here, because Calendar Sync (which runs before ExportData)
        // is responsible for checking pending status and syncing to calendar.
        // If we mark them synced here, Calendar sync would skip them on next run if it failed previously.

        // 4. Export Payments
        var paymentData = new List<IList<object>> { SheetSchema.PaymentHeader() };
        foreach (var p in payments)
        {
            paymentData.Add(SheetSchema.ToRow(p));
        }
        await _googleService.PushData("Payments!A1", paymentData);
        var unsyncedPayments = payments.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedPayments.Any())
        {
            foreach (var p in unsyncedPayments) p.SyncState = SyncState.Synced;
            await _localDb.SavePayments(unsyncedPayments);
        }

        // 5. Export Services
        var serviceData = new List<IList<object>> { SheetSchema.ServiceHeader() };
        foreach (var s in services)
        {
            serviceData.Add(SheetSchema.ToRow(s));
        }
        await _googleService.PushData("Services!A1", serviceData);
        var unsyncedServices = services.Where(x => x.SyncState != SyncState.Synced).ToList();
        if (unsyncedServices.Any())
        {
            foreach (var s in unsyncedServices) s.SyncState = SyncState.Synced;
            await _localDb.SaveServices(unsyncedServices);
        }
    }

    // Row shape lives in SheetSchema so the mapping can be round-trip tested.
}
