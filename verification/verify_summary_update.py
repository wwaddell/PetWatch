from playwright.sync_api import sync_playwright, expect
import datetime

def test_summary_page(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")

    # 2. Click Summary
    page.click("text=Summary")
    page.wait_for_url("**/summary")

    # 3. Verify Elements
    expect(page.get_by_role("heading", name="Financial Summary")).to_be_visible()

    # Check for Start Date and End Date pickers
    # MudDatePicker usually associates label with input
    start_picker = page.get_by_label("Start Date")
    end_picker = page.get_by_label("End Date")
    expect(start_picker).to_be_visible()
    expect(end_picker).to_be_visible()

    # Check for Quick Reset Buttons
    expect(page.get_by_role("button", name="This Year")).to_be_visible()
    expect(page.get_by_role("button", name="Last Year")).to_be_visible()
    expect(page.get_by_role("button", name="Last 3 Months")).to_be_visible()

    # 4. Test Interaction
    # Click "Last Year"
    page.click("text=Last Year")

    # Wait for UI update (small delay might be needed if async, but value check retries)

    # Verify dates
    # Last year is CurrentYear - 1
    current_year = datetime.datetime.now().year
    last_year = current_year - 1

    # Expected format yyyy-MM-dd
    expected_start = f"{last_year}-01-01"
    expected_end = f"{last_year}-12-31"

    # MudDatePicker input value attribute
    expect(start_picker).to_have_value(expected_start)
    expect(end_picker).to_have_value(expected_end)

    # 5. Click Apply Filter
    page.click("text=Apply Filter")

    # Verify Chart still exists
    expect(page.locator(".mud-chart")).to_be_visible()

    # Screenshot
    page.screenshot(path="verification/summary_updated.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_summary_page(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/summary_update_error.png")
            raise e
        finally:
            browser.close()
