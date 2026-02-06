import time
from playwright.sync_api import sync_playwright

def verify_ui():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        # Check Appointments Page
        print("Navigating to Appointments...")
        page.goto("http://localhost:5000/")
        # Wait for "Appointments" header
        page.wait_for_selector("h5:has-text('Appointments')")
        time.sleep(1) # Extra wait for rendering

        # Check Filter Label
        if page.get_by_text("Future/Unpaid").count() > 0:
            print("SUCCESS: Found filter label 'Future/Unpaid'")
        else:
            print("FAILURE: Did not find filter label 'Future/Unpaid'")

        page.screenshot(path="verification/appointments.png")
        print("Screenshot saved to verification/appointments.png")

        # Check Customers Page
        print("Navigating to Customers...")
        page.goto("http://localhost:5000/customers")
        # Wait for "Customers" header
        page.wait_for_selector("h5:has-text('Customers')")
        time.sleep(1)

        page.screenshot(path="verification/customers.png")
        print("Screenshot saved to verification/customers.png")

        browser.close()

if __name__ == "__main__":
    verify_ui()
