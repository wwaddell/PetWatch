import re

with open('PetSitterApp/Pages/Summary.razor', 'r') as f:
    content = f.read()

# Add IDialogService injection
content = content.replace(
    '@inject LocalDbService LocalDbService',
    '@inject LocalDbService LocalDbService\n@inject IDialogService DialogService'
)

# Update MudTable Paid row to make it clickable
content = content.replace(
    '<MudTd DataLabel="Paid">@context.Paid.ToString("C")</MudTd>',
    '<MudTd DataLabel="Paid"><MudLink OnClick="@(() => ShowPaidBreakdown(context))" Color="Color.Primary" Underline="Underline.Always">@context.Paid.ToString("C")</MudLink></MudTd>'
)

# Update MonthlySummaryData class
content = content.replace(
    'public double Paid { get; set; }\n    }',
    'public double Paid { get; set; }\n        public List<SummaryPaidBreakdownDialog.PaymentBreakdownItem> Payments { get; set; } = new();\n    }'
)

# In LoadData, fetch Customers and Pets
content = content.replace(
    'var payments = await LocalDbService.GetPayments();',
    'var payments = await LocalDbService.GetPayments();\n            _customers = await LocalDbService.GetCustomers();\n            _pets = await LocalDbService.GetPets();'
)

# In ProcessData, update signature and process payments
process_data_search = 'private void ProcessData(List<Appointment> appointments, List<Payment> payments)'
process_data_replace = '''private List<Customer> _customers = new();
    private List<Pet> _pets = new();

    private void ProcessData(List<Appointment> appointments, List<Payment> payments)'''

content = content.replace(process_data_search, process_data_replace)


loop_body_search = '''            var monthPaid = validPayments
                .Where(p => monthAppointmentIds.Contains(p.AppointmentId))
                .Sum(p => (double)p.Amount);

            paidData.Add(monthPaid);

            tableDataList.Add(new MonthlySummaryData
            {
                MonthDate = current,
                MonthLabel = current.ToString("MMM yy"),
                Expected = monthExpected,
                Paid = monthPaid
            });'''

loop_body_replace = '''            var currentMonthPayments = validPayments
                .Where(p => monthAppointmentIds.Contains(p.AppointmentId))
                .ToList();

            var monthPaid = currentMonthPayments.Sum(p => (double)p.Amount);
            paidData.Add(monthPaid);

            var breakdownItems = new List<SummaryPaidBreakdownDialog.PaymentBreakdownItem>();
            foreach (var p in currentMonthPayments)
            {
                var appointment = validAppointments.FirstOrDefault(a => a.Id == p.AppointmentId);
                var customerName = "";
                var petNames = "";
                if (appointment != null)
                {
                    var customer = _customers.FirstOrDefault(c => c.Id == appointment.CustomerId);
                    if (customer != null)
                    {
                        customerName = $"{customer.FirstName} {customer.LastName}";
                    }
                    var pets = _pets.Where(pet => appointment.PetIds.Contains(pet.Id)).Select(pet => pet.Name).ToList();
                    petNames = string.Join(", ", pets);
                }

                breakdownItems.Add(new SummaryPaidBreakdownDialog.PaymentBreakdownItem
                {
                    PaymentDate = p.PaymentDate,
                    CustomerName = customerName,
                    PetNames = petNames,
                    Amount = (double)p.Amount
                });
            }

            // Sort breakdown items by date
            breakdownItems = breakdownItems.OrderBy(b => b.PaymentDate).ToList();

            tableDataList.Add(new MonthlySummaryData
            {
                MonthDate = current,
                MonthLabel = current.ToString("MMM yy"),
                Expected = monthExpected,
                Paid = monthPaid,
                Payments = breakdownItems
            });'''

content = content.replace(loop_body_search, loop_body_replace)


show_paid_method = '''    private async Task ShowPaidBreakdown(MonthlySummaryData data)
    {
        var parameters = new DialogParameters<SummaryPaidBreakdownDialog>
        {
            { x => x.Payments, data.Payments }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        await DialogService.ShowAsync<SummaryPaidBreakdownDialog>($"Payments for {data.MonthLabel}", parameters, options);
    }
}'''

content = content.replace('}\n}', '}\n' + show_paid_method)


with open('PetSitterApp/Pages/Summary.razor', 'w') as f:
    f.write(content)
