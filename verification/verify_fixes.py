import time
from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    # 1. Load the app
    print("Loading app...")
    page.goto("http://localhost:5000")
    # Wait for initial load
    page.wait_for_timeout(3000)

    # 2. Add Customer with Pet
    print("Navigating to Customers...")
    page.get_by_role("link", name="Customers").click()
    page.wait_for_url("**/customers")
    page.wait_for_timeout(1000)

    print("Adding Customer...")
    page.get_by_role("button", name="Add Customer").click()

    # Fill Customer Details
    page.get_by_label("First Name").fill("John")
    page.get_by_label("Last Name").fill("Doe")
    page.get_by_label("Email").fill("john.doe@example.com")

    # Add Pet
    print("Adding Pet...")
    page.get_by_role("button", name="Add Pet").click()
    # Assuming PetDialog opens
    # Use exact=True to avoid matching "First Name", "Last Name"
    # Or scope to the top dialog.
    # Also "Name" might be "Name*" due to Required=true.
    # Let's try scoping to the last dialog.
    # The new dialog should be the last one in the DOM usually.
    # But Playwright matching might still be ambiguous if multiple exist.
    # Let's try exact match on "Name" or "Name*".
    # MudBlazor often renders label as "Name *".

    # We'll try to find the input inside the active dialog.
    # Assuming the active dialog is the one with "Name" input that is visible and enabled.

    # Let's try exact=True on "Name" and if that fails, we can try other things.
    # But wait, the error said: 3 elements.
    # 1) First Name
    # 2) Last Name
    # 3) Name*

    # So "First Name" contains "Name".
    # We want "Name".
    # If we use exact=True, "Name" should NOT match "First Name".
    # But it might not match "Name*" either if exact=True.

    # Let's try get_by_label("Name*") if exact=True fails on "Name".
    # Or rely on the fact that PetDialog is the LAST dialog.

    # Using specific selectors for clarity in this complex dialog stacking:
    # We can filter by placeholder if available, or just use the nth match if we know the order.
    # But robust way:
    # pet_dialog = page.get_by_role("dialog").last
    # pet_dialog.get_by_label("Name").fill("Fluffy")

    # Wait, get_by_role("dialog") returns the dialog container.
    # MudDialog uses role="dialog".

    # Let's try:
    page.locator("div.mud-dialog").last.get_by_label("Name").fill("Fluffy")

    # page.get_by_label("Species").fill("Dog") # Skip, default is Dog.
    # Save Pet
    # There might be two "Save" buttons now (one for PetDialog, one for CustomerDialog)
    # MudDialog usually stacks. The top one is the active one.
    # We can use get_by_role("button", name="Save").last or similar if ambiguous.
    # But usually the dialog is modal.
    page.get_by_role("button", name="Save").last.click()

    # Save Customer
    print("Saving Customer...")
    # After Pet Dialog closes, there is only one Save button left.
    page.get_by_role("button", name="Save").click()

    page.wait_for_timeout(1000)

    # 3. Verify Customer Name Format
    print("Verifying Customer Name Format...")
    # Should be "Doe, John"
    expect(page.get_by_text("Doe, John")).to_be_visible()

    # 4. Create Appointment
    print("Navigating to Appointments...")
    page.get_by_role("link", name="Appointments").click()
    # page.wait_for_url("**/appointments") # Default page is actually /, so it might be just /
    page.wait_for_timeout(1000) # Just wait a bit, it's SPA navigation.

    print("Adding Appointment...")
    page.get_by_role("button", name="Add Appointment").click()

    # Select Customer
    print("Selecting Customer...")
    # MudAutocomplete
    # It has a label "Customer".
    # Clicking it and typing "Doe"
    page.get_by_label("Customer").click()
    page.get_by_label("Customer").fill("Doe")
    page.wait_for_timeout(500)
    page.get_by_text("Doe, John").click()

    # Save Appointment
    page.get_by_role("button", name="Save").click()
    page.wait_for_timeout(1000)

    # 5. Verify Appointment Customer Link & Dialog
    print("Verifying Appointment Customer Link...")
    # Find the link "Doe, John" and click it
    # MudLink without Href might not be a role="link".
    page.get_by_text("Doe, John").first.click()

    # Verify Dialog Content
    print("Verifying Dialog Content...")
    # Header should be "Doe, John" (The Title of the dialog)
    # Content should contain "Fluffy"

    expect(page.get_by_role("dialog")).to_be_visible()
    # Check title. MudDialog title is often in a specific class or h6.
    # But get_by_text("Doe, John") should match the title.
    expect(page.get_by_role("dialog").get_by_text("Doe, John")).to_be_visible()
    # Check Pets
    expect(page.get_by_text("Fluffy")).to_be_visible()
    # Check "Contact" is NOT in header (it was removed)
    # We can check that the text "Contact Doe, John" is NOT visible.
    expect(page.get_by_text("Contact Doe, John")).not_to_be_visible()

    # Close Dialog
    page.get_by_role("button", name="Close").click()

    # 6. Verify Appointment Deletion
    print("Verifying Appointment Deletion...")
    # Find row. Note: MudDataGrid header is also a row.
    # We want the data row.
    # We can look for the row containing "Doe, John".
    row = page.get_by_role("row").filter(has_text="Doe, John").first
    # There are two buttons in Actions column: Edit and Delete.
    # Assuming Delete is the last button in the row.
    row.get_by_role("button").last.click()

    # Confirm deletion (The confirmation dialog button likely has text "Delete")
    page.get_by_role("button", name="Delete").click()

    page.wait_for_timeout(1000)
    # Should be gone
    expect(page.get_by_text("Doe, John")).not_to_be_visible()

    # Reload page to verify persistence (Soft Delete working)
    print("Reloading to verify persistence...")
    page.reload()
    page.wait_for_timeout(2000)
    expect(page.get_by_text("Doe, John")).not_to_be_visible()

    # 7. Verify Customer Deletion
    print("Navigating to Customers for Deletion...")
    page.get_by_role("link", name="Customers").click()
    page.wait_for_timeout(1000)

    # Delete Customer
    print("Deleting Customer...")
    row = page.get_by_role("row").filter(has_text="Doe, John").first
    row.get_by_role("button").last.click()

    page.get_by_role("button", name="Delete").click()

    page.wait_for_timeout(1000)
    # Should be gone
    expect(page.get_by_text("Doe, John")).not_to_be_visible()

    # Reload
    print("Reloading Customer list...")
    page.reload()
    page.wait_for_timeout(2000)
    expect(page.get_by_text("Doe, John")).not_to_be_visible()

    print("Taking screenshot...")
    page.screenshot(path="verification/verification.png")

    browser.close()
    print("Verification Done.")

if __name__ == "__main__":
    with sync_playwright() as playwright:
        run(playwright)
