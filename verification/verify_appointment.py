from playwright.sync_api import sync_playwright, expect
import time

def test_add_appointment(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))
    page.on("pageerror", lambda err: print(f"Page Error: {err}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")
    page.wait_for_selector("text=Pet Sitter App")

    # Setup: Add a customer and pet first to verify selection
    page.click("text=Customers")
    page.wait_for_url("**/customers")
    page.get_by_role("button", name="Add Customer").click()
    page.wait_for_selector(".mud-dialog")

    # Fill Customer info
    # The image shows "Add Customer" dialog is visible behind "Add Pet".
    # We must ensure we fill "Add Customer" fields before opening "Add Pet" if we want to save customer later?
    # Or just open Add Pet immediately.

    page.get_by_label("First Name").fill("Test")
    page.get_by_label("Last Name").fill("Owner")
    page.get_by_label("Email").fill("test@test.com")

    # Open Add Pet
    page.get_by_role("button", name="Add Pet").click()
    time.sleep(1) # Wait for animation

    # Verify Add Pet dialog is open
    # The screenshot shows "Add Pet" dialog on top.
    # It has fields "Name", "Species", "Breed", "Notes".
    # And buttons "CANCEL", "SAVE".

    # We need to target the *active* dialog.
    # .mud-dialog is likely used for both. The last one in DOM should be the top one.
    top_dialog = page.locator(".mud-dialog").last

    top_dialog.get_by_label("Name", exact=True).fill("Fluffy")
    top_dialog.get_by_label("Species").fill("Dog")

    # Click SAVE on Pet Dialog
    top_dialog.get_by_role("button", name="SAVE").click()

    # Wait for Pet Dialog to close?
    time.sleep(0.5)

    # Now we are back to "Add Customer" dialog.
    # Click SAVE on Customer Dialog.
    # Again, target the visible dialog (which should be the only one now, or the last one).
    page.locator(".mud-dialog").last.get_by_role("button", name="SAVE").click()

    # 2. Navigate to Appointments
    page.click("text=Appointments")
    page.wait_for_url("**/appointments")

    # 3. Add Appointment
    page.click("text=Add Appointment")
    page.wait_for_selector(".mud-dialog")

    appt_dialog = page.locator(".mud-dialog").last

    # 4. Fill form
    # Customer Select
    appt_dialog.locator(".mud-select").first.click()
    page.click("text=Test Owner")

    # Service Type Select (Index 2 likely, as per previous attempt)
    appt_dialog.locator(".mud-select").nth(2).click()
    page.click("text=House Sit")

    # Pets Select (Index 1)
    appt_dialog.locator(".mud-select").nth(1).click()
    page.click("text=Fluffy (Dog)")
    # Close dropdown
    page.locator(".mud-overlay").click()

    appt_dialog.get_by_label("Title").fill("Weekend Sit")

    # Rate
    appt_dialog.get_by_label("Rate").fill("50")

    # Dates
    appt_dialog.get_by_label("Start Date").fill("2023-10-27")
    appt_dialog.get_by_label("End Date").fill("2023-10-29")

    # Calculate
    appt_dialog.get_by_role("button", name="Calculate").click()

    # 5. Screenshot
    time.sleep(1)
    page.screenshot(path="verification/appointment_dialog.png")

    # 6. Save
    appt_dialog.get_by_role("button", name="SAVE").click()

    # 7. Verify listing
    expect(page.get_by_text("Weekend Sit")).to_be_visible()
    expect(page.get_by_text("House Sit")).to_be_visible()

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_add_appointment(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/appt_error_2.png")
        finally:
            browser.close()
