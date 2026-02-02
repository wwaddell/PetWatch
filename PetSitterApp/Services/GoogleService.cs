using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace PetSitterApp.Services;

public class GoogleService
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly HttpClient _httpClient;
    private readonly Microsoft.JSInterop.IJSRuntime _jsRuntime;
    private SheetsService? _sheetsService;
    private CalendarService? _calendarService;
    private string _spreadsheetId = string.Empty;
    private string _calendarId = string.Empty;
    private const string AppName = "PetSitterApp";

    public GoogleService(IAccessTokenProvider tokenProvider, HttpClient httpClient, Microsoft.JSInterop.IJSRuntime jsRuntime)
    {
        _tokenProvider = tokenProvider;
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
        if (tokenResult.TryGetToken(out var token))
        {
            return token.Value;
        }
        throw new Exception("Could not retrieve access token");
    }

    private async Task InitializeServicesAsync()
    {
        if (_sheetsService != null && _calendarService != null) return;

        var accessToken = await GetAccessTokenAsync();
        var credential = GoogleCredential.FromAccessToken(accessToken);

        _sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });

        _calendarService = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
    }

    public async Task EnsureSheetExists()
    {
        await InitializeServicesAsync();

        var storedId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "PetSitterSpreadsheetId");
        if (!string.IsNullOrEmpty(storedId))
        {
            _spreadsheetId = storedId;
            // Optionally verify it exists
            try
            {
                await _sheetsService!.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                return;
            }
            catch
            {
                // Invalid or deleted, create new
            }
        }

        _spreadsheetId = await CreateSpreadsheet();
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "PetSitterSpreadsheetId", _spreadsheetId);
    }

    // Helper to create a sheet
    public async Task<string> CreateSpreadsheet()
    {
        await InitializeServicesAsync();
        var spreadsheet = new Spreadsheet()
        {
            Properties = new SpreadsheetProperties()
            {
                Title = "PetSitterApp Data"
            }
        };
        var created = await _sheetsService!.Spreadsheets.Create(spreadsheet).ExecuteAsync();

        // Initialize sheets (tabs)
        await AddSheet(created.SpreadsheetId, "Customers");
        await AddSheet(created.SpreadsheetId, "Pets");
        await AddSheet(created.SpreadsheetId, "Appointments");
        await AddSheet(created.SpreadsheetId, "Payments");
        await AddSheet(created.SpreadsheetId, "Services");

        return created.SpreadsheetId;
    }

    private async Task AddSheet(string spreadsheetId, string title)
    {
         var batchUpdate = new BatchUpdateSpreadsheetRequest()
         {
             Requests = new List<Request>()
             {
                 new Request()
                 {
                     AddSheet = new AddSheetRequest()
                     {
                         Properties = new SheetProperties()
                         {
                             Title = title
                         }
                     }
                 }
             }
         };
         try
         {
            await _sheetsService!.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
         }
         catch
         {
             // Sheet might already exist (e.g. default "Sheet1" renamed or something)
         }
    }

    public async Task SyncToCalendar(PetSitterApp.Models.Appointment appointment)
    {
        await InitializeServicesAsync();

        // Find or create "PetSitterApp" calendar
        if (string.IsNullOrEmpty(_calendarId))
        {
            var calendars = await _calendarService!.CalendarList.List().ExecuteAsync();
            var existing = calendars.Items.FirstOrDefault(c => c.Summary == AppName);
            if (existing != null)
            {
                _calendarId = existing.Id;
            }
            else
            {
                var newCal = new Google.Apis.Calendar.v3.Data.Calendar { Summary = AppName };
                var created = await _calendarService.Calendars.Insert(newCal).ExecuteAsync();
                _calendarId = created.Id;
            }
        }

        var ev = new Event()
        {
            Summary = $"{appointment.ServiceType}", // Title removed, using ServiceType
            Description = $"{appointment.Description}\nService: {appointment.ServiceType}\nExpected: {appointment.ExpectedAmount:C}",
            Start = new EventDateTime() { Date = appointment.Start?.ToString("yyyy-MM-dd") },
            End = new EventDateTime() { Date = appointment.End?.ToString("yyyy-MM-dd") }
        };

        if (string.IsNullOrEmpty(appointment.GoogleEventId))
        {
             var createdEvent = await _calendarService!.Events.Insert(ev, _calendarId).ExecuteAsync();
             appointment.GoogleEventId = createdEvent.Id;
        }
        else
        {
            try
            {
                await _calendarService!.Events.Update(ev, _calendarId, appointment.GoogleEventId).ExecuteAsync();
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Event deleted in Google Calendar, recreate? Or clear ID?
                // For now, recreate
                var createdEvent = await _calendarService!.Events.Insert(ev, _calendarId).ExecuteAsync();
                appointment.GoogleEventId = createdEvent.Id;
            }
        }
    }

    public async Task PushData(string range, IList<IList<object>> values)
    {
        await InitializeServicesAsync();

        // Clear the sheet first to avoid artifacts
        var clearRequest = _sheetsService!.Spreadsheets.Values.Clear(new ClearValuesRequest(), _spreadsheetId, range.Split('!')[0]); // e.g., "Customers"
        await clearRequest.ExecuteAsync();

        var valueRange = new ValueRange { Values = values };
        var request = _sheetsService!.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();
    }

    public async Task<IList<IList<object>>?> ReadData(string range)
    {
        await InitializeServicesAsync();
        var request = _sheetsService!.Spreadsheets.Values.Get(_spreadsheetId, range);
        var response = await request.ExecuteAsync();
        return response.Values;
    }
}
