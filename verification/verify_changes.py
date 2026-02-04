from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    print("Navigating to root...")
    page.goto("http://localhost:5000/")

    # Verify we are on Appointments page
    try:
        expect(page.get_by_role("heading", name="Appointments")).to_be_visible(timeout=10000)
        print("Appointments header visible.")
    except Exception as e:
        print(f"Failed to find Appointments header: {e}")
        page.screenshot(path="verification/error_root.png")
        raise

    # Take screenshot of Appointments
    page.screenshot(path="verification/appointments_view.png")
    print("Appointments screenshot taken.")

    # Verify Home link is gone from Navigation
    # The new navigation has "Appointments" with href="/"
    # The old "Home" link with text "Home" should be gone.
    # Note: "Home" might appear in breadcrumbs or elsewhere, but the nav link specifically.
    # We can check that there is no link with name "Home".
    try:
        home_links = page.get_by_role("link", name="Home").count()
        if home_links == 0:
            print("Home link is correctly absent.")
        else:
            print(f"Warning: Found {home_links} Home links.")
    except Exception as e:
        print(f"Error checking home links: {e}")

    # Navigate to Settings
    print("Navigating to Settings...")
    page.goto("http://localhost:5000/settings")

    # Verify Version number
    # It was added as <MudText Typo="Typo.caption" ...>Version: 2024-05-22-01</MudText>
    try:
        expect(page.get_by_text("Version: 2024-05-22-01")).to_be_visible(timeout=10000)
        print("Version number visible.")
    except Exception as e:
        print(f"Failed to find Version number: {e}")
        page.screenshot(path="verification/error_settings.png")
        raise

    # Take screenshot of Settings
    page.screenshot(path="verification/settings_view.png")
    print("Settings screenshot taken.")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
