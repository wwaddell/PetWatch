using System.Globalization;
using PetSitterApp.Models;
using PetSitterApp.Services;

namespace PetSitterApp.Tests;

/// <summary>
/// The spreadsheet is the only durable copy of this app's data, and a sync
/// merges by replacing local records with what came back from the sheet. A
/// field missing from the schema is therefore silent data loss, which is
/// exactly what these tests exist to catch.
/// </summary>
public class SheetSchemaTests
{
    // ------------------------------------------------------------- Round trips
    //
    // Every property that survives a sync must come back unchanged. If someone
    // adds a model field and forgets the schema, the matching test fails.

    [Fact]
    public void Customer_survives_a_round_trip()
    {
        var original = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            PhoneNumber = "555-0100",
            Address = "1 Main St",
            Notes = "Gate code 1234",
            IsDeleted = true,
            UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };

        var result = SheetSchema.ToCustomer(SheetSchema.ToRow(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.FirstName, result.FirstName);
        Assert.Equal(original.LastName, result.LastName);
        Assert.Equal(original.Email, result.Email);
        Assert.Equal(original.PhoneNumber, result.PhoneNumber);
        Assert.Equal(original.Address, result.Address);
        Assert.Equal(original.Notes, result.Notes);
        Assert.Equal(original.IsDeleted, result.IsDeleted);
        Assert.Equal(original.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void Pet_survives_a_round_trip_including_the_dates_and_delete_flag()
    {
        var original = new Pet
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Name = "Rex",
            Species = "Dog",
            Breed = "Lab",
            Notes = "Likes walks",
            IsDeleted = true,
            DateOfBirth = new DateTime(2020, 5, 1),
            DateOfDeath = new DateTime(2031, 6, 2),
            UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };

        var result = SheetSchema.ToPet(SheetSchema.ToRow(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.CustomerId, result.CustomerId);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Species, result.Species);
        Assert.Equal(original.Breed, result.Breed);
        Assert.Equal(original.Notes, result.Notes);
        Assert.Equal(original.IsDeleted, result.IsDeleted);
        Assert.Equal(original.DateOfBirth, result.DateOfBirth);
        Assert.Equal(original.DateOfDeath, result.DateOfDeath);
        Assert.Equal(original.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void Service_survives_a_round_trip_including_the_delete_flag()
    {
        var original = new ServiceModel
        {
            Id = Guid.NewGuid(),
            Name = "Overnight",
            DefaultRate = 87.50m,
            IsMultiplePerDay = true,
            IsObsolete = true,
            IsDeleted = true,
            UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };

        var result = SheetSchema.ToService(SheetSchema.ToRow(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.DefaultRate, result.DefaultRate);
        Assert.Equal(original.IsMultiplePerDay, result.IsMultiplePerDay);
        Assert.Equal(original.IsObsolete, result.IsObsolete);
        Assert.Equal(original.IsDeleted, result.IsDeleted);
        Assert.Equal(original.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void Appointment_survives_a_round_trip_including_VisitsPerDay()
    {
        var original = new Appointment
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            PetIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
            Description = "Two dogs, back gate",
            Start = new DateTime(2026, 1, 5),
            End = new DateTime(2026, 1, 8),
            ServiceType = "Overnight",
            VisitsPerDay = 3,
            Rate = 42.25m,
            ExpectedAmount = 380.25m,
            GoogleEventId = "evt-123",
            IsDeleted = true,
            UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };

        var result = SheetSchema.ToAppointment(SheetSchema.ToRow(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.CustomerId, result.CustomerId);
        Assert.Equal(original.PetIds, result.PetIds);
        Assert.Equal(original.Description, result.Description);
        Assert.Equal(original.Start, result.Start);
        Assert.Equal(original.End, result.End);
        Assert.Equal(original.ServiceType, result.ServiceType);
        Assert.Equal(original.VisitsPerDay, result.VisitsPerDay);
        Assert.Equal(original.Rate, result.Rate);
        Assert.Equal(original.ExpectedAmount, result.ExpectedAmount);
        Assert.Equal(original.GoogleEventId, result.GoogleEventId);
        Assert.Equal(original.IsDeleted, result.IsDeleted);
        Assert.Equal(original.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void Payment_survives_a_round_trip()
    {
        var original = new Payment
        {
            Id = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            Amount = 125.75m,
            Method = "Write off",
            PaymentDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            Notes = "partial",
            IsDeleted = true,
            UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
        };

        var result = SheetSchema.ToPayment(SheetSchema.ToRow(original));

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.AppointmentId, result.AppointmentId);
        Assert.Equal(original.Amount, result.Amount);
        Assert.Equal(original.Method, result.Method);
        Assert.Equal(original.PaymentDate, result.PaymentDate);
        Assert.Equal(original.Notes, result.Notes);
        Assert.Equal(original.IsDeleted, result.IsDeleted);
        Assert.Equal(original.UpdatedAt, result.UpdatedAt);
    }

    // ----------------------------------------------------------- Column widths
    //
    // Header, row and read range must be the same width. A mismatch shifts
    // every field silently, which is far worse than a hard failure.

    public static TheoryData<string, int, int> SheetWidths() => new()
    {
        { SheetSchema.CustomersRange,    SheetSchema.CustomerHeader().Count,    SheetSchema.ToRow(new Customer()).Count },
        { SheetSchema.PetsRange,         SheetSchema.PetHeader().Count,         SheetSchema.ToRow(new Pet()).Count },
        { SheetSchema.ServicesRange,     SheetSchema.ServiceHeader().Count,     SheetSchema.ToRow(new ServiceModel()).Count },
        { SheetSchema.AppointmentsRange, SheetSchema.AppointmentHeader().Count, SheetSchema.ToRow(new Appointment()).Count },
        { SheetSchema.PaymentsRange,     SheetSchema.PaymentHeader().Count,     SheetSchema.ToRow(new Payment()).Count },
    };

    [Theory]
    [MemberData(nameof(SheetWidths))]
    public void Header_row_and_range_are_the_same_width(string range, int headerWidth, int rowWidth)
    {
        // "Pets!A2:J" -> J -> 10 columns
        var lastColumn = range[^1];
        var rangeWidth = lastColumn - 'A' + 1;

        Assert.Equal(rangeWidth, headerWidth);
        Assert.Equal(rangeWidth, rowWidth);
    }

    // ------------------------------------------------------------ Column names
    //
    // Regression cover for an export that aborted part-way: the tail-clear range
    // named column ZZ, and a tab is 26 columns wide by default, so Sheets
    // rejected the call with "exceeds grid limits". The first tab was written
    // and every later tab was skipped, while the app reported success.

    [Theory]
    [InlineData(1, "A")]
    [InlineData(7, "G")]
    [InlineData(10, "J")]
    [InlineData(13, "M")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    public void ColumnName_maps_a_column_number_to_A1_notation(int column, string expected)
    {
        Assert.Equal(expected, SheetSchema.ColumnName(column));
    }

    [Fact]
    public void TailClearRange_stops_at_the_last_written_column()
    {
        // The exact regression: this used to be "Pets!A11:ZZ".
        Assert.Equal("Pets!A11:J", SheetSchema.TailClearRange("Pets", rowsWritten: 10, width: 10));
        Assert.Equal("Customers!A2:I", SheetSchema.TailClearRange("Customers", rowsWritten: 1, width: 9));
        Assert.Equal("Appointments!A4:M", SheetSchema.TailClearRange("Appointments", rowsWritten: 3, width: 13));
    }

    [Theory]
    [MemberData(nameof(SheetWidths))]
    public void TailClearRange_never_names_a_column_past_the_grid(string range, int headerWidth, int rowWidth)
    {
        var sheet = range.Split('!')[0];
        var tail = SheetSchema.TailClearRange(sheet, rowsWritten: 5, width: headerWidth);

        // Everything after the ':' must be within A..Z for a default 26 wide tab.
        var lastColumn = tail.Split(':')[1];
        Assert.True(lastColumn.Length == 1 && lastColumn[0] is >= 'A' and <= 'Z',
            $"{tail} names column {lastColumn}, which is past the default 26 column grid.");
        Assert.Equal(headerWidth, rowWidth);
    }

    [Theory]
    [MemberData(nameof(SheetWidths))]
    public void No_sheet_is_wider_than_the_default_26_column_grid(string range, int headerWidth, int rowWidth)
    {
        // A range naming a column past the end of the grid is rejected outright
        // rather than clamped, which aborts the export.
        Assert.True(headerWidth <= 26, $"{range} needs {headerWidth} columns; the default grid is 26 wide.");
        Assert.Equal(headerWidth, rowWidth);
    }

    [Theory]
    [MemberData(nameof(SheetWidths))]
    public void The_range_last_column_matches_what_ColumnName_derives(string range, int headerWidth, int rowWidth)
    {
        // PushData derives its clear range from the row width, so that width and
        // the declared read range have to name the same final column.
        Assert.Equal(range[^1].ToString(), SheetSchema.ColumnName(headerWidth));
        Assert.Equal(headerWidth, rowWidth);
    }

    // ----------------------------------------------- Backward compatible reads
    //
    // Sheets written before a column existed have short rows. Those must still
    // load, with the absent columns taking a sensible fallback.

    [Fact]
    public void Pet_row_from_the_old_seven_column_schema_still_loads()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var oldRow = new List<object>
        {
            id.ToString(), customerId.ToString(), "Rex", "Dog", "Lab", "notes", "2026-03-04 05:06:07"
        };

        var pet = SheetSchema.ToPet(oldRow);

        Assert.Equal(id, pet.Id);
        Assert.Equal("Rex", pet.Name);
        Assert.False(pet.IsDeleted);
        Assert.Null(pet.DateOfBirth);
        Assert.Null(pet.DateOfDeath);
    }

    [Fact]
    public void Appointment_row_without_VisitsPerDay_keeps_the_model_default_not_zero()
    {
        var oldRow = new List<object>
        {
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "2026-01-05", "2026-01-08",
            "desc", "Visit", "10", "30", "", "2026-03-04 05:06:07", "FALSE", ""
        };

        var appointment = SheetSchema.ToAppointment(oldRow);

        // Zero here would silently zero out the cost calculation in RULES.md 3.
        Assert.Equal(1, appointment.VisitsPerDay);
    }

    [Fact]
    public void Service_row_from_the_old_six_column_schema_still_loads()
    {
        var oldRow = new List<object>
        {
            Guid.NewGuid().ToString(), "Walk", "25", "FALSE", "FALSE", "2026-03-04 05:06:07"
        };

        var service = SheetSchema.ToService(oldRow);

        Assert.Equal("Walk", service.Name);
        Assert.False(service.IsDeleted);
    }

    [Fact]
    public void A_completely_empty_row_does_not_crash_the_readers()
    {
        var empty = new List<object>();

        // The id is the merge key, so an unreadable one must fail loudly rather
        // than quietly importing a record under Guid.Empty.
        Assert.Throws<FormatException>(() => SheetSchema.ToPet(empty));
    }

    [Fact]
    public void A_malformed_id_is_reported_rather_than_silently_remapped()
    {
        var row = new List<object> { "not-a-guid", Guid.NewGuid().ToString(), "Rex" };

        var ex = Assert.Throws<FormatException>(() => SheetSchema.ToPet(row));
        Assert.Contains("Pet", ex.Message);
    }

    // ------------------------------------------------------------------ Culture
    //
    // Sheets can return values formatted to the spreadsheet locale, and the app
    // runs in whatever culture the browser reports.

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]   // 1.234,56 and dd.MM.yyyy
    [InlineData("fr-FR")]
    public void Round_trips_are_stable_across_cultures(string cultureName)
    {
        var original = CultureScope.Run(cultureName, () =>
        {
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Start = new DateTime(2026, 1, 5),
                End = new DateTime(2026, 12, 8),
                Rate = 1234.56m,
                ExpectedAmount = 9876.54m,
                VisitsPerDay = 2,
                UpdatedAt = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc)
            };

            var result = SheetSchema.ToAppointment(SheetSchema.ToRow(appointment));

            Assert.Equal(appointment.Start, result.Start);
            Assert.Equal(appointment.End, result.End);
            Assert.Equal(appointment.Rate, result.Rate);
            Assert.Equal(appointment.ExpectedAmount, result.ExpectedAmount);
            Assert.Equal(appointment.UpdatedAt, result.UpdatedAt);
            return appointment;
        });

        Assert.NotEqual(Guid.Empty, original.Id);
    }

    [Fact]
    public void A_date_returned_in_us_sheet_formatting_is_still_understood()
    {
        // Sheets may hand back "5/1/2020" rather than the "2020-05-01" written.
        var row = new List<object>
        {
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Rex", "Dog", "Lab", "",
            "2026-03-04 05:06:07", "FALSE", "5/1/2020", ""
        };

        var pet = CultureScope.Run("en-US", () => SheetSchema.ToPet(row));

        Assert.Equal(new DateTime(2020, 5, 1), pet.DateOfBirth);
        Assert.Null(pet.DateOfDeath);
    }

    // ------------------------------------------------------- Locale'd numbers
    //
    // Regression cover for a 100x error: with group separators allowed, an
    // invariant read of "1234,56" succeeds as 123456 instead of deferring to
    // the culture-aware read.

    [Theory]
    [InlineData("en-US", "1234.56")]
    [InlineData("en-US", "1,234.56")]
    [InlineData("de-DE", "1234,56")]
    [InlineData("de-DE", "1.234,56")]
    [InlineData("fr-FR", "1234,56")]
    public void A_rate_formatted_for_the_sheet_locale_reads_back_intact(string cultureName, string cell)
    {
        var row = new List<object> { Guid.NewGuid().ToString(), "Overnight", cell, "FALSE", "FALSE", "" };

        var service = CultureScope.Run(cultureName, () => SheetSchema.ToService(row));

        Assert.Equal(1234.56m, service.DefaultRate);
    }

    [Fact]
    public void A_decimal_cell_object_is_stringified_invariantly_not_by_locale()
    {
        // Google hands back boxed values; ToString() under de-DE would render
        // 1234.56m as "1234,56" and the reader would then see 123456.
        var row = new List<object> { Guid.NewGuid().ToString(), "Overnight", 1234.56m, false, false, "" };

        var service = CultureScope.Run("de-DE", () => SheetSchema.ToService(row));

        Assert.Equal(1234.56m, service.DefaultRate);
    }

    // ------------------------------------------------------------------ PetIds

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,")]
    public void An_appointment_with_no_pets_reads_as_an_empty_list(string petIds)
    {
        var row = new List<object>
        {
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "", "", "", "", "0", "0", petIds
        };

        Assert.Empty(SheetSchema.ToAppointment(row).PetIds);
    }

    [Fact]
    public void Unreadable_pet_ids_are_dropped_rather_than_failing_the_appointment()
    {
        var good = Guid.NewGuid();
        var row = new List<object>
        {
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "", "", "", "", "0", "0",
            $"{good},not-a-guid"
        };

        Assert.Equal(new[] { good }, SheetSchema.ToAppointment(row).PetIds);
    }

    [Fact]
    public void Timestamps_without_an_offset_are_read_as_utc_not_local()
    {
        var row = new List<object>
        {
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Rex", "", "", "",
            "2026-03-04 05:06:07"
        };

        var pet = SheetSchema.ToPet(row);

        Assert.Equal(DateTimeKind.Utc, pet.UpdatedAt.Kind);
        Assert.Equal(new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc), pet.UpdatedAt);
    }

    private static class CultureScope
    {
        public static T Run<T>(string cultureName, Func<T> body)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                return body();
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }
    }
}
