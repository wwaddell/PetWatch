from playwright.sync_api import sync_playwright, expect
import time

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    page.on("console", lambda msg: print(f"PAGE LOG: {msg.text}"))
    page.on("pageerror", lambda err: print(f"PAGE ERROR: {err}"))
    page.on("requestfailed", lambda request: print(f"REQUEST FAILED: {request.url} - {request.failure}"))
    # page.on("request", lambda request: print(f"REQUEST: {request.url}"))

    # 1. Navigate to Settings
    print("Navigating to Settings...")
    page.goto("http://localhost:5000/settings")

    # 2. Click Login
    print("Clicking Log in...")
    page.get_by_role("button", name="Log in with Google").click()

    # 3. Wait a bit
    time.sleep(5)
    print(f"Current URL: {page.url}")

    try:
        page.wait_for_url("https://accounts.google.com/**", timeout=5000)
        print("Redirected to Google Accounts successfully.")
    except Exception as e:
        print(f"Failed to redirect to Google: {page.url}")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
