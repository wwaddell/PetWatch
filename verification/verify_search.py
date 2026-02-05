from playwright.sync_api import sync_playwright, expect
import time
import datetime

def test_search(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))
    page.on("pageerror", lambda err: print(f"Page Error: {err}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")
    page.wait_for_selector("text=Pet Sitter App")

    # 2. Add Customers
    # Click sidebar link. Use specific selector to avoid clicking title.
    page.click("aside .mud-nav-link-text:has-text('Customers')")
    page.wait_for_url("**/customers")

    # Check if customers exist, if not add
    # We add unique names to avoid confusion with existing data
    suffix = str(int(time.time()))
    alice_name = f"Alice{suffix}"
    bob_name = f"Bob{suffix}"

    add_customer(page, alice_name, "Search", f"alice{suffix}@example.com")
    add_customer(page, bob_name, "Ignore", f"bob{suffix}@example.com")

    # 3. Test Customer Search
    print("Testing Customer Search...")
    search_input = page.get_by_placeholder("Search by name")
    search_input.fill(alice_name)

    time.sleep(1) # Allow filter to apply

    # Expect Alice to be visible
    expect(page.get_by_text(f"Search, {alice_name}")).to_be_visible()
    # Expect Bob to be hidden
    expect(page.get_by_text(f"Ignore, {bob_name}")).not_to_be_visible()

    # Clear search
    search_input.fill("")
    time.sleep(1)
    expect(page.get_by_text(f"Ignore, {bob_name}")).to_be_visible()
    print("Customer Search Passed")
    page.screenshot(path="verification/search_customer_success.png")

    # 4. Add Appointment for Alice
    print("Adding Appointment...")
    page.click("aside .mud-nav-link-text:has-text('Appointments')")
    # Link goes to /, so waiting for **/appointments might fail if it is just /.
    # We wait for the heading "Appointments" to appear.
    expect(page.get_by_role("heading", name="Appointments")).to_be_visible()

    page.get_by_role("button", name="Add Appointment").click()
    page.wait_for_selector(".mud-dialog")

    # Select Customer
    # MudAutocomplete input with Label="Customer"
    # Usually the label is associated with the input.
    # We can try to click the input.
    # The label "Customer" is likely a label element.
    # We can try filling the input that is near the label.
    page.get_by_label("Customer").fill(alice_name)
    # Wait for dropdown
    page.click(f"text=Search, {alice_name}")

    # Fill dates (optional if defaults are ok, but let's be safe)
    # Defaults are Now and Now+1h.
    # We need to ensure we can save.
    # "Please select start and end dates" - defaults should be set in code.
    # But let's just click Save.
    page.get_by_role("button", name="Save").click()
    page.wait_for_selector(".mud-dialog", state="hidden")
    time.sleep(1)

    # 5. Test Appointment Search
    print("Testing Appointment Search...")

    # By default, "Future or Unpaid Only" is checked.
    # The new appointment is for today (Now), so it should be visible.

    search_input_appt = page.get_by_placeholder("Search by name")
    search_input_appt.fill(alice_name)
    time.sleep(1)

    # Verify Alice's appointment is shown
    # The grid usually shows the customer name link.
    # MudLink with OnClick might not be a 'link' role if it has no href. Use text.
    expect(page.get_by_text(f"Search, {alice_name}")).to_be_visible()

    # Verify we don't see random other stuff?
    # Hard to verify "not visible" if we don't know what else is there.
    # But we can verify that if we search for "Bob", we see nothing (since Bob has no appointment).

    search_input_appt.fill(bob_name)
    time.sleep(1)
    expect(page.get_by_text(f"Search, {alice_name}")).not_to_be_visible()
    # expect(page.get_by_text("No records found")).to_be_visible() # Text might vary

    print("Appointment Search Passed")
    page.screenshot(path="verification/search_appointment_success.png")

def add_customer(page, first, last, email):
    page.get_by_role("button", name="Add Customer").click()
    page.wait_for_selector(".mud-dialog")
    page.get_by_label("First Name").fill(first)
    page.get_by_label("Last Name").fill(last)
    page.get_by_label("Email").fill(email)
    page.get_by_label("Phone Number").fill("555-0123")
    page.get_by_role("button", name="Save").click()
    page.wait_for_selector(".mud-dialog", state="hidden")
    time.sleep(0.5)

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_search(page)
            print("All verification steps passed.")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/search_fail.png")
            exit(1)
        finally:
            browser.close()
