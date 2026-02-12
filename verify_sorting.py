from playwright.sync_api import sync_playwright
import time

def verify_sorting():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to the app
        print("Navigating to app...")
        page.goto("http://localhost:5000")

        # Wait for the app to load
        page.wait_for_selector("text=Appointments", timeout=10000)

        # Inject Data
        print("Injecting data...")
        page.evaluate("""
            () => {
                return new Promise((resolve, reject) => {
                    const dbName = 'PetSitterDB';
                    const dbVersion = 3;
                    const request = indexedDB.open(dbName, dbVersion);

                    request.onsuccess = (event) => {
                        const db = event.target.result;

                        const customers = [
                            { id: '11111111-1111-1111-1111-111111111111', FirstName: 'Alice', LastName: 'Adams', Email: 'alice@example.com', PhoneNumber: '123', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 },
                            { id: '22222222-2222-2222-2222-222222222222', FirstName: 'Charlie', LastName: 'Baker', Email: 'charlie@example.com', PhoneNumber: '123', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 },
                            { id: '33333333-3333-3333-3333-333333333333', FirstName: 'Bob', LastName: 'Adams', Email: 'bob@example.com', PhoneNumber: '123', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 }
                        ];

                        const pets = [
                            { id: '44444444-4444-4444-4444-444444444444', Name: 'Zoe', Species: 'Dog', Breed: 'Lab', CustomerId: '11111111-1111-1111-1111-111111111111', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 },
                            { id: '55555555-5555-5555-5555-555555555555', Name: 'Albert', Species: 'Cat', Breed: 'Tabby', CustomerId: '22222222-2222-2222-2222-222222222222', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 },
                            { id: '66666666-6666-6666-6666-666666666666', Name: 'Max', Species: 'Dog', Breed: 'Poodle', CustomerId: '33333333-3333-3333-3333-333333333333', IsDeleted: false, CreatedAt: new Date(), UpdatedAt: new Date(), SyncState: 0 }
                        ];

                        const tx = db.transaction(['customers', 'pets'], 'readwrite');
                        const custStore = tx.objectStore('customers');
                        const petStore = tx.objectStore('pets');

                        customers.forEach(c => custStore.put(c));
                        pets.forEach(p => petStore.put(p));

                        tx.oncomplete = () => resolve('Data injected');
                        tx.onerror = (e) => reject(e.target.error);
                    };

                    request.onerror = (e) => reject(e.target.error);
                });
            }
        """)

        # Reload to pick up data
        print("Reloading...")
        page.reload()
        page.wait_for_selector("text=Appointments", timeout=10000)

        # Verify Customers
        print("Verifying Customers...")
        page.goto("http://localhost:5000/customers")

        # Wait for data grid to load
        page.wait_for_selector("text=Customers", timeout=10000)
        page.wait_for_selector("tr.mud-table-row", timeout=5000)

        # Check order visually via screenshot
        page.screenshot(path="verification_customers.png")
        print("Taken verification_customers.png")

        # Also grab the text of the first column to programmatically verify
        names = page.locator("tr.mud-table-row > td:nth-child(1)").all_inner_texts()
        print(f"Customer Names: {names}")

        # Verify Pets
        print("Verifying Pets...")
        page.goto("http://localhost:5000/pets")

        # Wait for data grid to load
        page.wait_for_selector("text=Pets", timeout=10000)
        page.wait_for_selector("tr.mud-table-row", timeout=5000)

        # Check order visually via screenshot
        page.screenshot(path="verification_pets.png")
        print("Taken verification_pets.png")

        pet_names = page.locator("tr.mud-table-row > td:nth-child(1)").all_inner_texts()
        print(f"Pet Names: {pet_names}")

        browser.close()

if __name__ == "__main__":
    verify_sorting()
