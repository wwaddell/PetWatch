from playwright.sync_api import sync_playwright, expect
import time

def verify_payment_dialog():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context()
        page = context.new_page()

        try:
            # Navigate to the app
            page.goto("http://localhost:5000/appointments")

            # Wait for app to load
            page.wait_for_selector(".mud-main-content", timeout=10000)
            time.sleep(2)

            # Find the specific add appointment button by its position or explicit parent
            buttons = page.locator("button.mud-button-root.mud-icon-button").all()
            for btn in buttons:
                # Get the inner HTML to check for the + icon SVG path
                html = btn.inner_html()
                if 'M19,3H5C3.89' in html or 'M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z' in html:
                    btn.click()
                    break

            time.sleep(2)

            # Check if appointment dialog opened
            if page.locator(".mud-dialog").count() == 0:
                print("Failed to open appointment dialog, trying another way")
                page.evaluate("document.querySelector('.mud-main-content button.mud-icon-button').click()")
                time.sleep(2)

            # Click Add Payment
            buttons = page.locator("button").all()
            for btn in buttons:
                text = btn.inner_text()
                if 'ADD PAYMENT' in text or 'Add Payment' in text:
                    btn.click()
                    break

            time.sleep(2)

            # Verify the dialog is open using heading
            expect(page.get_by_role("heading", name="Add Payment")).to_be_visible()

            # Take screenshot of the initial state showing empty amount
            page.screenshot(path="verification_payment_dialog_initial.png")

            # Try to save without entering an amount
            # The second dialog is the Add Payment dialog
            dialogs = page.locator(".mud-dialog").all()
            if len(dialogs) > 1:
                payment_dialog = dialogs[1]
            else:
                payment_dialog = dialogs[0]

            save_button = payment_dialog.locator("button", has_text="Save")
            if save_button.count() == 0:
                save_button = payment_dialog.locator("button", has_text="SAVE")
            save_button.first.click()

            time.sleep(1)

            # Verify error message is shown
            expect(page.get_by_text("Please enter an amount greater than 0.")).to_be_visible()

            # Take screenshot with error
            page.screenshot(path="verification_payment_dialog_error.png")

            print("Verification successful!")

        except Exception as e:
            print(f"Error: {e}")
            page.screenshot(path="verification_payment_dialog_error_state.png")
            raise
        finally:
            browser.close()

if __name__ == "__main__":
    verify_payment_dialog()
