from playwright.sync_api import sync_playwright, expect
import time

def test_add_customer(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))
    page.on("pageerror", lambda err: print(f"Page Error: {err}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")
    page.wait_for_selector("text=Pet Sitter App")

    # 2. Navigate to Customers
    page.click("text=Customers")
    page.wait_for_url("**/customers")

    # 3. Add Customer
    # Use generic text match or role
    page.get_by_role("button", name="Add Customer").click()

    # Wait for dialog
    page.wait_for_selector(".mud-dialog")

    # 4. Fill form
    # MudBlazor inputs often have the label as a sibling or parent, but let's try get_by_label if accessible
    # Or use placeholder if label is tricky.
    # MudTextField with Label="First Name" usually creates a label element.
    page.get_by_label("First Name").fill("John")
    page.get_by_label("Last Name").fill("Doe")
    page.get_by_label("Email").fill("john@example.com")
    page.get_by_label("Phone Number").fill("555-0123")

    # Wait a bit for bindings?
    time.sleep(0.5)

    # 5. Save
    page.get_by_role("button", name="Save").click()

    # Wait for dialog to close
    page.wait_for_selector(".mud-dialog", state="hidden")

    # 6. Verify listing
    # Reload page to be sure? No, should update automatically.
    # But if LocalDb is slow, maybe wait?
    expect(page.get_by_text("John Doe")).to_be_visible()

    # Screenshot
    page.screenshot(path="verification/customer_added.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_add_customer(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/error_2.png")
        finally:
            browser.close()
