from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    page.on("console", lambda msg: print(f"PAGE LOG: {msg.text}"))
    page.on("pageerror", lambda err: print(f"PAGE ERROR: {err}"))

    # 1. Navigate to Settings
    print("Navigating to Settings...")
    page.goto("http://localhost:5000/settings")

    # 2. Click Login
    print("Clicking Log in...")
    page.get_by_role("button", name="Log in with Google").click()

    # 3. Expect Navigation to Google (or at least not 404)
    # Since we can't actually log in to Google in headless/sandbox without credentials,
    # we just want to verify it redirects to accounts.google.com

    try:
        page.wait_for_url("https://accounts.google.com/**", timeout=5000)
        print("Redirected to Google Accounts successfully.")
    except Exception as e:
        print(f"Failed to redirect to Google: {page.url}")
        # If it stayed on localhost but showed 404, that's bad.
        if "authentication/login" in page.url:
             print("Stuck on authentication/login - likely 404 if Authentication.razor is missing/broken.")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
