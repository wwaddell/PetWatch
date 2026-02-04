from playwright.sync_api import sync_playwright, expect
import time

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()

    print("Navigating to root...")
    page.goto("http://localhost:5000/")

    print("Opening Add Appointment Dialog...")
    page.get_by_role("button", name="Add Appointment").click()

    # Verify Calculator Icon
    # MudIconButton usually has the icon class.
    # We look for a button that has the icon inside.
    # The icon string is "Icons.Material.Filled.Calculate" which translates to an SVG with specific path or class.
    # MudBlazor renders icons as SVGs.
    # We can assume if there is an IconButton near "Expected Amount", it's the one.
    # Or check if text "Calculate" is gone.

    try:
        calculate_text = page.get_by_role("button", name="Calculate").count()
        if calculate_text == 0:
            print("Calculate text button is correctly absent.")
        else:
            print("Error: Calculate text button still present.")
    except:
        pass

    # Take screenshot of dialog
    page.screenshot(path="verification/appointment_dialog_icon.png")
    print("Dialog screenshot taken.")

    # Close dialog (Cancel)
    page.get_by_role("button", name="Cancel").click()

    # Navigate to Settings
    print("Navigating to Settings...")
    page.goto("http://localhost:5000/settings")

    # Verify Version
    try:
        expect(page.get_by_text("Version: 2024-05-23")).to_be_visible(timeout=10000)
        print("Version 2024-05-23 visible.")
    except Exception as e:
        print(f"Failed to find Version: {e}")
        page.screenshot(path="verification/error_version.png")

    # Test Update Toast
    print("Triggering Update Available...")
    page.evaluate("DotNet.invokeMethodAsync('PetSitterApp', 'OnUpdateAvailable')")

    # Wait for toast
    try:
        # MudSnackbar
        expect(page.get_by_text("A new version is available.")).to_be_visible(timeout=5000)
        expect(page.get_by_role("button", name="Update")).to_be_visible()
        print("Update toast appeared.")
        page.screenshot(path="verification/update_toast.png")
    except Exception as e:
        print(f"Update toast failed: {e}")
        page.screenshot(path="verification/error_toast.png")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
