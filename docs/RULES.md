# Application Business Rules

This document outlines the key business rules and logic used within the PetSitterApp. It serves as a reference for both users understanding application behavior and developers maintaining the codebase.

## 1. Financial Summary

The Financial Summary page (`/summary`) aggregates data to show expected earnings versus actual payments.

### Date Filtering
*   **Expected Total:** Calculated based on the **Appointment Start Date**. An appointment falls within the selected period if its Start Date is within the range.
*   **Paid Total:** Calculated based on the **Payment Date**. A payment falls within the selected period if its Payment Date is within the range.
*   **"Last 3 Months" Logic:** This filter selects a date range starting from the **1st day of the month, 2 months prior** to the current date, ending on the **last day of the current month**.
    *   *Example:* If today is March 15th, the range is January 1st to March 31st.

### Exclusions
*   **Write-offs:** Payments with the method set to "Write off" (case-insensitive) are **excluded** from the "Paid Total" calculation.

## 2. Pet Birthdays

The application highlights upcoming birthdays in the Pets list (`/pets`).

### Calculation Logic
*   **Closest Birthday:** The system calculates the difference between today's date and the pet's birthday for the previous year, current year, and next year to find the absolute closest occurrence.
*   **"Approaching" Definition:** A birthday is considered approaching if the closest occurrence is within **10 days** (inclusive) of the current date.
*   **Visual Indicator:** A "Celebration" icon appears in the Actions column for pets with approaching birthdays.

## 3. Appointment Logic

### Cost Calculation
The total expected amount for an appointment is calculated using the following formula:

```
Duration (Days) * Visits Per Day * Rate
```

*   **Duration (Days):** Calculated as `(End Date - Start Date)`.
    *   *Minimum Duration:* The system enforces a minimum duration of **1 day**. Even if the start and end times are on the same day, it counts as 1 day.
    *   *Hotel-Style Logic:* The calculation effectively counts "nights". For example, Jan 1st to Jan 2nd is 1 day.
*   **Visits Per Day:** Defined by the selected Service (default) but can be overridden per appointment.
*   **Rate:** The cost per visit/day.

### "Active" Appointments
On the Appointments list (`/appointments`), the "Active" filter shows appointments that meet **either** of the following criteria:
1.  **Future Date:** The appointment `End Date` is greater than or equal to today (`>= Today`).
2.  **Unpaid Balance:** The appointment has a remaining balance greater than $0 (Expected Amount > Total Paid).

## 4. Google Calendar Integration

The application syncs appointments to a Google Calendar named "PetSitterApp".

*   **Event Title:** Constructed as: `[Customer Name] - [Pet Names] - [Service Type]`.
    *   If no pets are associated, it uses: `[Customer Name] - [Service Type]`.
*   **Event Date:**
    *   **Start:** Matches the Appointment `Start Date`.
    *   **End:** Set to `Appointment End Date + 1 Day`. This is because Google Calendar treats all-day event end dates as *exclusive* (the event ends at midnight on that day, effectively covering the day before).
*   **Description:** Includes details such as Visits Per Day, Expected Amount, and any notes from the Appointment, Customer, or Pets.
