from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    page.on("console", lambda msg: print(f"PAGE LOG: {msg.text}"))
    page.on("pageerror", lambda err: print(f"PAGE ERROR: {err}"))

    # 1. Navigate to Settings
    print("Navigating to Settings...")
    page.goto("http://localhost:5000/settings")

    # Wait for loading to finish
    expect(page.get_by_role("heading", name="Settings")).to_be_visible()

    # 2. Check for Yellow Bar
    print("Checking for Yellow Bar...")
    # The yellow bar has id "blazor-error-ui" and class "dismiss" inside it, or text "Reload"
    # It is usually present in DOM but hidden (display: none).
    # We want to ensure it is NOT visible.

    error_ui = page.locator("#blazor-error-ui")

    # If it is visible, it's a failure.
    if error_ui.is_visible():
        print("Yellow bar IS visible!")
        # If visible, check text
        print(f"Text: {error_ui.inner_text()}")
    else:
        print("Yellow bar is NOT visible.")

    expect(error_ui).not_to_be_visible()

    # 3. Screenshot
    print("Taking screenshot...")
    page.screenshot(path="verification/verification_settings.png")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
