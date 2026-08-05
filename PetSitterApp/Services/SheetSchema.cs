using PetSitterApp.Models;
using System.Globalization;

namespace PetSitterApp.Services;

/// <summary>
/// The single definition of how each entity maps to a row of the Google Sheet.
///
/// Three things must agree for every entity: the read range width, the header,
/// and the row builder. SheetSchemaTests round-trips each entity and asserts
/// those widths match, because a mismatch shifts every field silently.
///
/// New columns are always APPENDED, never inserted, and every reader tolerates
/// a short row - so a sheet written by an older build still loads, with the
/// absent columns coming back as their documented fallback.
/// </summary>
public static class SheetSchema
{
    public const string CustomersRange = "Customers!A2:I";
    public const string PetsRange = "Pets!A2:J";
    public const string ServicesRange = "Services!A2:G";
    public const string AppointmentsRange = "Appointments!A2:M";
    public const string PaymentsRange = "Payments!A2:H";

    // ---------------------------------------------------------------- Customers

    public static IList<object> CustomerHeader() => new List<object>
        { "Id", "FirstName", "LastName", "Email", "Phone", "Address", "IsDeleted", "UpdatedAt", "Notes" };

    public static IList<object> ToRow(Customer c) => new List<object>
        { c.Id.ToString(), c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.Address, c.IsDeleted, ToUtcString(c.UpdatedAt), c.Notes };

    public static Customer ToCustomer(IList<object> row) => new()
    {
        Id = ParseGuid(row, 0, nameof(Customer)),
        FirstName = Cell(row, 1),
        LastName = Cell(row, 2),
        Email = Cell(row, 3),
        PhoneNumber = Cell(row, 4),
        Address = Cell(row, 5),
        IsDeleted = ParseBool(row, 6),
        UpdatedAt = ParseUtcDate(row, 7),
        Notes = Cell(row, 8)
    };

    // --------------------------------------------------------------------- Pets

    public static IList<object> PetHeader() => new List<object>
        { "Id", "CustomerId", "Name", "Species", "Breed", "Notes", "UpdatedAt", "IsDeleted", "DateOfBirth", "DateOfDeath" };

    public static IList<object> ToRow(Pet p) => new List<object>
        { p.Id.ToString(), p.CustomerId.ToString(), p.Name, p.Species, p.Breed, p.Notes, ToUtcString(p.UpdatedAt), p.IsDeleted, ToDateString(p.DateOfBirth), ToDateString(p.DateOfDeath) };

    public static Pet ToPet(IList<object> row) => new()
    {
        Id = ParseGuid(row, 0, nameof(Pet)),
        CustomerId = ParseGuid(row, 1, nameof(Pet)),
        Name = Cell(row, 2),
        Species = Cell(row, 3),
        Breed = Cell(row, 4),
        Notes = Cell(row, 5),
        UpdatedAt = ParseUtcDate(row, 6),
        IsDeleted = ParseBool(row, 7),
        DateOfBirth = ParseNullableDate(row, 8),
        DateOfDeath = ParseNullableDate(row, 9)
    };

    // ----------------------------------------------------------------- Services

    public static IList<object> ServiceHeader() => new List<object>
        { "Id", "Name", "DefaultRate", "IsMultiplePerDay", "IsObsolete", "UpdatedAt", "IsDeleted" };

    public static IList<object> ToRow(ServiceModel s) => new List<object>
        { s.Id.ToString(), s.Name, s.DefaultRate, s.IsMultiplePerDay, s.IsObsolete, ToUtcString(s.UpdatedAt), s.IsDeleted };

    public static ServiceModel ToService(IList<object> row) => new()
    {
        Id = ParseGuid(row, 0, nameof(ServiceModel)),
        Name = Cell(row, 1),
        DefaultRate = ParseDecimal(row, 2),
        IsMultiplePerDay = ParseBool(row, 3),
        IsObsolete = ParseBool(row, 4),
        UpdatedAt = ParseUtcDate(row, 5),
        IsDeleted = ParseBool(row, 6)
    };

    // ------------------------------------------------------------- Appointments

    public static IList<object> AppointmentHeader() => new List<object>
        { "Id", "CustomerId", "Start", "End", "Description", "ServiceType", "Rate", "ExpectedAmount", "PetIds", "UpdatedAt", "IsDeleted", "GoogleEventId", "VisitsPerDay" };

    public static IList<object> ToRow(Appointment a) => new List<object>
    {
        a.Id.ToString(),
        a.CustomerId.ToString(),
        ToDateString(a.Start),
        ToDateString(a.End),
        a.Description,
        a.ServiceType,
        a.Rate,
        a.ExpectedAmount,
        string.Join(",", a.PetIds),
        ToUtcString(a.UpdatedAt),
        a.IsDeleted,
        a.GoogleEventId ?? "",
        a.VisitsPerDay
    };

    public static Appointment ToAppointment(IList<object> row)
    {
        var a = new Appointment
        {
            Id = ParseGuid(row, 0, nameof(Appointment)),
            CustomerId = ParseGuid(row, 1, nameof(Appointment)),
            Start = ParseNullableDate(row, 2),
            End = ParseNullableDate(row, 3),
            Description = Cell(row, 4),
            ServiceType = Cell(row, 5),
            Rate = ParseDecimal(row, 6),
            ExpectedAmount = ParseDecimal(row, 7),
            PetIds = ParseGuidList(row, 8),
            UpdatedAt = ParseUtcDate(row, 9),
            IsDeleted = ParseBool(row, 10),
            GoogleEventId = Cell(row, 11)
        };

        // Absent from sheets written before VisitsPerDay was added, so keep the
        // model default rather than collapsing those appointments to zero.
        a.VisitsPerDay = ParseInt(row, 12, a.VisitsPerDay);
        return a;
    }

    // ----------------------------------------------------------------- Payments

    public static IList<object> PaymentHeader() => new List<object>
        { "Id", "AppointmentId", "Amount", "Method", "Date", "Notes", "UpdatedAt", "IsDeleted" };

    public static IList<object> ToRow(Payment p) => new List<object>
        { p.Id.ToString(), p.AppointmentId.ToString(), p.Amount, p.Method, ToUtcString(p.PaymentDate), p.Notes, ToUtcString(p.UpdatedAt), p.IsDeleted };

    public static Payment ToPayment(IList<object> row) => new()
    {
        Id = ParseGuid(row, 0, nameof(Payment)),
        AppointmentId = ParseGuid(row, 1, nameof(Payment)),
        Amount = ParseDecimal(row, 2),
        Method = Cell(row, 3),
        PaymentDate = ParseUtcDate(row, 4),
        Notes = Cell(row, 5),
        UpdatedAt = ParseUtcDate(row, 6),
        IsDeleted = ParseBool(row, 7)
    };

    // ------------------------------------------------------------------ Writers

    public static string ToUtcString(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        return dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string ToDateString(DateTime? dt) =>
        dt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    /// <summary>
    /// Range covering everything below the rows just written, used to truncate a
    /// tab after a push.
    ///
    /// Bounded at the last column actually written. An earlier version ran to
    /// column ZZ, which is past the end of a default 26 column grid; Sheets
    /// rejects such a range instead of clamping it, which aborted the export
    /// after the first tab.
    /// </summary>
    public static string TailClearRange(string sheetName, int rowsWritten, int width) =>
        $"{sheetName}!A{rowsWritten + 1}:{ColumnName(width)}";

    /// <summary>
    /// A1 column name for a 1-based column number: 1 -> A, 26 -> Z, 27 -> AA.
    ///
    /// Needed so a range never names a column past the end of the grid. A tab is
    /// 26 columns wide by default, and Sheets rejects a range beyond that with
    /// "exceeds grid limits" rather than clamping it.
    /// </summary>
    public static string ColumnName(int oneBasedColumn)
    {
        if (oneBasedColumn < 1) throw new ArgumentOutOfRangeException(nameof(oneBasedColumn));

        var name = "";
        while (oneBasedColumn > 0)
        {
            var remainder = (oneBasedColumn - 1) % 26;
            name = (char)('A' + remainder) + name;
            oneBasedColumn = (oneBasedColumn - 1) / 26;
        }
        return name;
    }

    // ------------------------------------------------------------------ Readers

    /// <summary>
    /// Index-safe cell read. Missing or null cells read as "".
    ///
    /// Non-string cells are formatted with the invariant culture on purpose. A
    /// plain ToString() would render a decimal as "1234,56" under a German or
    /// French locale, which then reads back as 123456.
    /// </summary>
    public static string Cell(IList<object> row, int index)
    {
        if (index >= row.Count) return "";

        return row[index] switch
        {
            null => "",
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            var other => other.ToString() ?? ""
        };
    }

    /// <summary>
    /// Ids are the merge key, so a malformed one is fatal for the row rather
    /// than something to paper over with Guid.Empty.
    /// </summary>
    public static Guid ParseGuid(IList<object> row, int index, string entity)
    {
        var text = Cell(row, index);
        if (Guid.TryParse(text, out var value)) return value;
        throw new FormatException($"{entity} row has an unreadable id in column {index + 1}: '{text}'");
    }

    public static Guid[] ParseGuidList(IList<object> row, int index)
    {
        var text = Cell(row, index);
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<Guid>();

        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
                   .Where(id => id != Guid.Empty)
                   .ToArray();
    }

    public static bool ParseBool(IList<object> row, int index, bool fallback = false) =>
        bool.TryParse(Cell(row, index), out var value) ? value : fallback;

    public static int ParseInt(IList<object> row, int index, int fallback) =>
        int.TryParse(Cell(row, index), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    /// <summary>
    /// Written as a plain number, but Sheets may return it formatted to the
    /// spreadsheet locale.
    ///
    /// The first attempt deliberately forbids group separators. Allowing them
    /// would make invariant parsing of "1234,56" succeed as 123456 - a silent
    /// 100x error - instead of falling through to the culture-aware read.
    /// </summary>
    public static decimal ParseDecimal(IList<object> row, int index, decimal fallback = 0m)
    {
        var text = Cell(row, index);
        if (string.IsNullOrWhiteSpace(text)) return fallback;

        const NumberStyles unambiguous =
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowCurrencySymbol;
        const NumberStyles permissive = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

        if (decimal.TryParse(text, unambiguous, CultureInfo.InvariantCulture, out var invariant)) return invariant;
        if (decimal.TryParse(text, permissive, CultureInfo.CurrentCulture, out var local)) return local;
        return decimal.TryParse(text, permissive, CultureInfo.InvariantCulture, out var loose) ? loose : fallback;
    }

    /// <summary>
    /// Timestamps are written as UTC. Anything without an offset is treated as
    /// UTC rather than as the reader's local time.
    /// </summary>
    public static DateTime ParseUtcDate(IList<object> row, int index) =>
        ParseUtcDate(row, index, DateTime.UtcNow);

    public static DateTime ParseUtcDate(IList<object> row, int index, DateTime fallback)
    {
        var text = Cell(row, index);
        if (string.IsNullOrWhiteSpace(text)) return fallback;

        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, styles, out var invariant)) return invariant;
        return DateTime.TryParse(text, CultureInfo.CurrentCulture, styles, out var local) ? local : fallback;
    }

    /// <summary>
    /// Written as yyyy-MM-dd, but Sheets can hand a date back formatted to the
    /// spreadsheet locale, so fall back to a culture-sensitive read.
    /// </summary>
    public static DateTime? ParseNullableDate(IList<object> row, int index)
    {
        var text = Cell(row, index);
        if (string.IsNullOrWhiteSpace(text)) return null;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant))
        {
            return invariant;
        }
        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var local) ? local : null;
    }
}
