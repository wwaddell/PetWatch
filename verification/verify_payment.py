from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    # 1. Navigate to Appointments
    print("Navigating to Appointments...")
    page.goto("http://localhost:5000/appointments")

    # Wait for loading to finish (if any)
    page.wait_for_selector("text=Appointments", timeout=10000)

    # 2. Add Appointment
    print("Opening Add Appointment dialog...")
    page.get_by_role("button", name="Add Appointment").click()
    expect(page.get_by_role("heading", name="Add Appointment")).to_be_visible()

    # 3. Add Payment
    print("Opening Add Payment dialog...")
    page.get_by_role("button", name="Add Payment").click()
    expect(page.get_by_role("heading", name="Add Payment")).to_be_visible()

    # 4. Fill Payment Details
    print("Filling payment details...")
    page.get_by_label("Amount", exact=True).fill("50")
    # Method defaults to Cash, that's fine.

    # 5. Save Payment
    print("Saving payment...")
    # There are multiple Save buttons, we want the one in the topmost dialog (PaymentDialog)
    # The PaymentDialog is the last one added to DOM usually.
    # Or we can click by position or refine search.
    # MudBlazor dialogs: .mud-dialog
    # Let's try finding the "Save" button within the "Add Payment" dialog context if possible,
    # but Playwright sees the whole page.
    # We can use the fact that the second "Save" button is the one we want.
    # Or just click the last visible "Save" button.
    save_buttons = page.get_by_role("button", name="Save")
    count = save_buttons.count()
    if count > 1:
        save_buttons.nth(count - 1).click()
    else:
        save_buttons.click()

    # 6. Verify Payment Listed
    print("Verifying payment added...")
    expect(page.get_by_text("$50.00")).to_be_visible()

    # 7. Click Delete Icon
    print("Clicking delete icon...")
    # The delete button is an IconButton in the list item.
    # It might have an aria-label or just an icon.
    # We can look for the button inside the list item that contains "$50.00"
    payment_item = page.locator(".mud-list-item").filter(has_text="$50.00")
    delete_btn = payment_item.locator("button") # The only button in the item is the delete button
    delete_btn.click()

    # 8. Verify Confirmation Dialog
    print("Verifying confirmation dialog...")
    expect(page.get_by_text("Delete Payment")).to_be_visible()
    expect(page.get_by_text("Are you sure you want to delete this payment?")).to_be_visible()

    # 9. Screenshot
    print("Taking screenshot...")
    page.screenshot(path="verification/verification_payment.png")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
