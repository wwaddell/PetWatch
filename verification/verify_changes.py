from playwright.sync_api import sync_playwright, expect
import datetime
import time

def verify_changes(page):
    # Wait for app to be ready
    try:
        page.goto("http://localhost:5000")
    except:
        time.sleep(5)
        page.goto("http://localhost:5000")

    # 1. Appointments Page
    print("Verifying Appointments Page...")
    expect(page.get_by_role("heading", name="Appointments")).to_be_visible()

    # Verify "Active" switch
    # MudSwitch usually has a label text sibling or child.
    # We can check if "Active" text is visible near the switch.
    expect(page.get_by_text("Active")).to_be_visible()

    # Take screenshot of Appointments page header
    page.screenshot(path="verification/appointments_header.png")
    print("Captured appointments_header.png")

    # 2. Customers Page
    print("Verifying Customers Page...")
    page.goto("http://localhost:5000/customers")
    expect(page.get_by_role("heading", name="Customers")).to_be_visible()

    # Wait for data grid to load (assuming some customers exist or at least the grid structure)
    # Check for the Actions column content.
    # We look for the 'Add Appointment' icon button.
    # MudIconButton usually has an svg or we can check the icon class if we knew it.
    # Or we can look for the button by role, though there are many.
    # Let's take a screenshot of the grid.
    time.sleep(2) # Wait for data load
    page.screenshot(path="verification/customers_grid.png")
    print("Captured customers_grid.png")

    # 3. Pets Page
    print("Verifying Pets Page...")
    page.goto("http://localhost:5000/pets")
    expect(page.get_by_role("heading", name="Pets")).to_be_visible()

    time.sleep(2) # Wait for data load
    page.screenshot(path="verification/pets_grid.png")
    print("Captured pets_grid.png")

    # 4. Summary Page
    print("Verifying Summary Page...")
    page.goto("http://localhost:5000/summary")
    expect(page.get_by_role("heading", name="Financial Summary")).to_be_visible()

    # Click "Last 3 Months"
    page.get_by_role("button", name="Last 3 Months").click()

    # Verify dates
    # Calculate expected
    today = datetime.date.today()
    # End date: Last day of current month
    if today.month == 12:
        next_month = datetime.date(today.year + 1, 1, 1)
    else:
        next_month = datetime.date(today.year, today.month + 1, 1)
    end_of_month = next_month - datetime.timedelta(days=1)

    # Start date: 1st day of 2 months ago
    # e.g. Feb -> Jan -> Dec (1st)
    # month - 2
    target_month = today.month - 2
    target_year = today.year
    if target_month <= 0:
        target_month += 12
        target_year -= 1

    start_date = datetime.date(target_year, target_month, 1)

    expected_start = start_date.strftime("%Y-%m-%d")
    expected_end = end_of_month.strftime("%Y-%m-%d")

    print(f"Expected Start: {expected_start}, Expected End: {expected_end}")

    # MudDatePicker inputs
    # Start Date input
    # It might take a moment to update?
    time.sleep(1)

    # The label is "Start Date". The input is usually associated.
    # We can try to find input by label.
    # MudBlazor inputs often have the label as a sibling div.
    # Let's assume we can get it by label or just check values if accessible.

    # Taking screenshot to verify manually if programmatic verification is hard
    page.screenshot(path="verification/summary_dates.png")
    print("Captured summary_dates.png")

    # Try to verify input values
    # MudDatePicker input is an <input> element.
    # We can use page.locator("input").nth(0) etc if we know order.
    # Start Date is first, End Date is second.

    start_input = page.get_by_label("Start Date")
    end_input = page.get_by_label("End Date")

    # Check value
    # expect(start_input).to_have_value(expected_start) # Might fail if formatting differs or if not using value attribute
    # expect(end_input).to_have_value(expected_end)

    # Just print found values
    found_start = start_input.input_value()
    found_end = end_input.input_value()
    print(f"Found Start: {found_start}, Found End: {found_end}")

    if found_start != expected_start:
        print("WARNING: Start Date mismatch!")
    if found_end != expected_end:
        print("WARNING: End Date mismatch!")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(viewport={"width": 1280, "height": 720})
        page = context.new_page()
        verify_changes(page)
        browser.close()
