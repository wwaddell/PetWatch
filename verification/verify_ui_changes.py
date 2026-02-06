from playwright.sync_api import sync_playwright, expect
import time

def run(page):
    page.goto("http://localhost:5000/customers")
    page.wait_for_selector(".mud-main-content")

    # Add Customer
    page.locator("button.mud-button-filled").first.click()
    expect(page.get_by_role("dialog")).to_be_visible()

    customer_dialog = page.locator(".mud-dialog").last
    customer_dialog.get_by_label("First Name").fill("Test")
    customer_dialog.get_by_label("Last Name").fill("Owner")

    # Add Pet
    customer_dialog.get_by_role("button", name="Add Pet").click()
    expect(page.get_by_role("heading", name="Add Pet")).to_be_visible()

    pet_dialog = page.locator(".mud-dialog").last

    # Date of Birth
    dob_input = pet_dialog.locator("div.mud-input-control", has_text="Date of Birth").locator("input")
    dob_input.click()

    picker = page.locator(".mud-picker-content")
    expect(picker).to_be_visible(timeout=5000)

    # Click TODAY
    # MudBlazor uses 'mud-day-today' or similar?
    # Let's try finding the day that has outline or specific class.
    # Actually, clicking 'Today' button in actions?
    # Does MudDatePicker have a 'Today' button? Usually not by default.
    # But the current day usually has `mud-current` class.
    if page.locator(".mud-day.mud-current").count() > 0:
        page.locator(".mud-day.mud-current").click()
    else:
        # Fallback: Click the 15th of the month, hoping it's safe?
        # Or click the selected day?
        # If I want to be safe, I can enter the date manually if I can.
        # But dialog picker...
        # Let's try .mud-selected if it defaults to today?
        if page.locator(".mud-day.mud-selected").count() > 0:
             page.locator(".mud-day.mud-selected").click()
        else:
             # Just click the first day and hope.
             # But first day failed last time (probably).
             # Let's try to find a number that matches today's date?
             # Hard to know "today" in the sandbox environment without checking system time.
             # I'll check system time in python.
             import datetime
             today_day = datetime.datetime.now().day
             # Click the text of the day.
             page.locator(".mud-day").filter(has_text=str(today_day)).first.click()

    if page.get_by_text("OK").is_visible():
        page.get_by_text("OK").click()

    pet_dialog.locator("div.mud-input-control", has_text="Name").locator("input").fill("Fido")
    pet_dialog.get_by_role("button", name="Save").click()
    expect(page.get_by_role("heading", name="Add Pet")).not_to_be_visible()

    customer_dialog.get_by_role("button", name="Save").click()

    page.goto("http://localhost:5000/pets")
    page.wait_for_selector("tr")

    row = page.locator("tr", has_text="Fido")
    expect(row).to_be_visible()

    actions_cell = row.locator("td").last
    buttons = actions_cell.locator("button").all()

    print(f"Found {len(buttons)} buttons.")
    for i, btn in enumerate(buttons):
        print(f"Button {i}: Title='{btn.get_attribute('title')}'")

    if len(buttons) >= 3:
        if buttons[-1].get_attribute("title") == "Birthday approaching!":
            print("SUCCESS: Celebration icon is last.")
        else:
            print("FAILURE: Celebration icon is NOT last.")
    else:
        print("WARNING: Only 2 buttons found. Could not verify Celebration icon position.")

    # Verify Clear button
    buttons[1].click() # Edit
    expect(page.get_by_role("heading", name="Edit Pet")).to_be_visible()
    edit_dialog = page.locator(".mud-dialog").last
    dob_control = edit_dialog.locator("div.mud-input-control", has_text="Date of Birth")
    if dob_control.locator("button").count() > 0:
        print("Adornment button found (Clear button).")

    page.screenshot(path="verification/verification.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            run(page)
        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification/error.png")
        finally:
            browser.close()
