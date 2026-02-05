from playwright.sync_api import sync_playwright, expect
import time

def test_summary_page(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))

    # 1. Navigate to Home
    page.goto("http://localhost:5000")

    # 2. Click Summary
    # The NavLink likely has text "Summary".
    page.click("text=Summary")
    page.wait_for_url("**/summary")

    # 3. Verify Elements
    expect(page.get_by_role("heading", name="Financial Summary")).to_be_visible()

    # Check for Chart
    # MudChart renders an SVG. We can check for a class or selector.
    # The output has mud-chart or similar.
    # Note: MudChart class is `mud-chart`.
    expect(page.locator(".mud-chart")).to_be_visible()

    # Check for Date Picker
    # MudDateRangePicker usually has an input with label "Date Range".
    expect(page.get_by_label("Date Range")).to_be_visible()

    # Screenshot
    page.screenshot(path="verification/summary_page.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_summary_page(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/summary_error.png")
            raise e
        finally:
            browser.close()
