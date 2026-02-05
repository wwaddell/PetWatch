from playwright.sync_api import sync_playwright, expect
import time

def test_appointment_lookup(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")
    expect(page.get_by_role("heading", name="Appointments")).to_be_visible()

    # 2. Add a Customer
    page.click("text=Customers")
    page.wait_for_url("**/customers")

    page.get_by_role("button", name="Add Customer").click()
    page.wait_for_selector(".mud-dialog")

    timestamp = str(int(time.time()))
    customer_name = f"PerfUser {timestamp}"

    page.get_by_label("First Name").fill("PerfUser")
    page.get_by_label("Last Name").fill(timestamp)
    page.get_by_role("button", name="Save").click()
    page.wait_for_selector(".mud-dialog", state="hidden")
    expect(page.get_by_text(customer_name)).to_be_visible()

    # 3. Add an Appointment
    page.click("text=Appointments")
    page.get_by_role("button", name="Add Appointment").click()
    page.wait_for_selector(".mud-dialog")

    # Select Customer
    page.get_by_text("Select Customer...").click()

    # Click the option by text
    page.click(f"text={customer_name}")

    # Save
    page.get_by_role("button", name="Save").click()
    page.wait_for_selector(".mud-dialog", state="hidden")

    # 4. Verify Customer Name in Grid
    expect(page.get_by_text(customer_name)).to_be_visible()

    # Screenshot
    page.screenshot(path="verification/appointment_lookup.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_appointment_lookup(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/appointment_error.png")
            raise e
        finally:
            browser.close()
