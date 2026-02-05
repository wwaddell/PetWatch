from playwright.sync_api import sync_playwright, expect

def test_summary_layout(page):
    page.on("console", lambda msg: print(f"Console: {msg.text}"))

    # 1. Navigate to Summary
    page.goto("http://localhost:5000/summary")

    # 2. Check for the Chart
    expect(page.locator(".mud-chart")).to_be_visible()

    # 3. Verify it is inside a Grid Item with correct class
    # The parent of the chart (or grand parent) should be a div with class mud-grid-item-md-8
    # We can inspect the DOM hierarchy.

    # Locate the chart SVG container
    chart = page.locator(".mud-chart")

    # Check if a parent has the grid item class
    # The chart is inside a MudItem which renders as a div with mud-grid-item... classes.
    # Note: MudItem classes are usually `mud-grid-item mud-grid-item-xs-12 ...`

    # We can try to select the parent directly.
    # XPath: select parent of element with class mud-chart
    parent = chart.locator("xpath=..")

    # We expect the parent (or close ancestor) to have class 'mud-grid-item-md-8'
    # Actually, MudBlazor classes are `mud-grid-item-md-8`.

    # Let's check if there is an element with that class that contains the chart.
    grid_item = page.locator(".mud-grid-item-md-8")
    expect(grid_item).to_be_visible()
    expect(grid_item).to_contain_text("Expected") # The legend text inside the chart

    print("Layout verification passed: Chart is inside mud-grid-item-md-8")

    # Screenshot
    page.screenshot(path="verification/summary_layout.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_summary_layout(page)
            print("Verification successful")
        except Exception as e:
            print(f"Verification failed: {e}")
            page.screenshot(path="verification/summary_layout_error.png")
            raise e
        finally:
            browser.close()
