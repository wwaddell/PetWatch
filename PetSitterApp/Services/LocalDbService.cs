using Microsoft.JSInterop;
using PetSitterApp.Models;

namespace PetSitterApp.Services;

public class LocalDbService
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _dbModule;

    public LocalDbService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task EnsureModuleLoaded()
    {
        if (_dbModule == null)
        {
            _dbModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/db.js");
            await _dbModule.InvokeVoidAsync("openDb");
        }
    }

    public async Task SaveCustomer(Customer customer)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("putRecord", "customers", customer);
    }

    public async Task<List<Customer>> GetCustomers()
    {
        await EnsureModuleLoaded();
        return await _dbModule!.InvokeAsync<List<Customer>>("getAllRecords", "customers");
    }

    public async Task DeleteCustomer(Guid id)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("deleteRecord", "customers", id);
    }

    public async Task SavePet(Pet pet)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("putRecord", "pets", pet);
    }

    public async Task<List<Pet>> GetPets()
    {
        await EnsureModuleLoaded();
        return await _dbModule!.InvokeAsync<List<Pet>>("getAllRecords", "pets");
    }

    public async Task DeletePet(Guid id)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("deleteRecord", "pets", id);
    }

    public async Task SaveAppointment(Appointment appointment)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("putRecord", "appointments", appointment);
    }

    public async Task<List<Appointment>> GetAppointments()
    {
        await EnsureModuleLoaded();
        return await _dbModule!.InvokeAsync<List<Appointment>>("getAllRecords", "appointments");
    }

    public async Task DeleteAppointment(Guid id)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("deleteRecord", "appointments", id);
    }

    public async Task SavePayment(Payment payment)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("putRecord", "payments", payment);
    }

    public async Task<List<Payment>> GetPayments()
    {
        await EnsureModuleLoaded();
        return await _dbModule!.InvokeAsync<List<Payment>>("getAllRecords", "payments");
    }

    public async Task DeletePayment(Guid id)
    {
        await EnsureModuleLoaded();
        await _dbModule!.InvokeVoidAsync("deleteRecord", "payments", id);
    }
}
