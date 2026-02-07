from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    # Use mobile viewport to match user scenario
    context = browser.new_context(viewport={"width": 390, "height": 844})
    page = context.new_page()

    try:
        page.goto("http://localhost:5000/customers")

        # Wait for title
        # MudText renders generic typography classes usually, maybe h5
        expect(page.get_by_role("heading", name="Customers")).to_be_visible()

        # Inject a large element to force scrolling
        # We target .mud-container which holds the @Body content
        page.evaluate("""
            const div = document.createElement('div');
            div.style.height = '2000px';
            div.style.backgroundColor = '#f0f0f0';
            div.innerText = 'Forcing Scroll';
            document.querySelector('.mud-container').appendChild(div);
        """)

        # Scroll down
        page.evaluate("window.scrollBy(0, 300)")
        page.wait_for_timeout(500)

        # Check visibility and position
        # The header text "Customers" is inside the sticky div
        header = page.get_by_role("heading", name="Customers")

        box = header.bounding_box()
        print(f"Header Y: {box['y']}")

        # App bar is usually 64px. So sticky header should be around 64px.
        # If it scrolled away, it would be around 64 - 300 = -236 (if it was at top)
        # Actually it starts a bit lower due to padding/margin.

        if box['y'] < 0:
            print("FAILURE: Header scrolled out of view.")
        else:
            print(f"SUCCESS: Header is visible at Y={box['y']}")

        page.screenshot(path="verification/sticky_customers.png")

    except Exception as e:
        print(f"Error: {e}")
        page.screenshot(path="verification/error.png")
    finally:
        browser.close()

with sync_playwright() as playwright:
    run(playwright)
