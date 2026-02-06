from playwright.sync_api import sync_playwright, expect

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        print("Navigating to settings...")
        page.goto("http://localhost:5200/settings")

        # Wait for the version text.
        # It's in a MudText with caption typo.
        # "Version: 2024.12.31-1159"

        print("Checking for version text...")
        locator = page.get_by_text("Version: 2024.12.31-1159")
        try:
            expect(locator).to_be_visible(timeout=10000)
            print("Version text found.")
        except Exception as e:
            print(f"Version text not found: {e}")
            print(page.content())
            raise

        page.screenshot(path="verification/settings_version.png")
        print("Screenshot taken.")
        browser.close()

if __name__ == "__main__":
    run()
