import asyncio
from playwright.async_api import async_playwright
import sys

async def run():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        page = await browser.new_page()

        print("Navigating to app...")
        try:
            await page.goto("http://localhost:5000", wait_until="networkidle")
        except Exception as e:
            print(f"Error navigating: {e}")
            return

        print("Injecting verification script...")
        try:
            result = await page.evaluate("""async () => {
                // Spy on indexedDB.open
                let openCalls = 0;
                const originalOpen = indexedDB.open.bind(indexedDB);
                indexedDB.open = function(...args) {
                    openCalls++;
                    console.log('indexedDB.open called');
                    return originalOpen(...args);
                };

                // Dynamic import
                const dbModule = await import('./js/db.js');

                // Ensure DB is open
                await dbModule.openDb();

                // Calls during initial open
                const initialCalls = openCalls;
                console.log(`Initial calls: ${initialCalls}`);

                // Measure putRecord performance
                const start = performance.now();
                const iterations = 100;
                const promises = [];
                for (let i = 0; i < iterations; i++) {
                    const id = crypto.randomUUID();
                    promises.push(dbModule.putRecord('customers', { id: id, name: 'Test ' + i }));
                }
                await Promise.all(promises);
                const duration = performance.now() - start;

                return {
                    initialCalls,
                    subsequentCalls: openCalls - initialCalls,
                    duration,
                    iterations
                };
            }""")

            print("-" * 30)
            print(f"Initial DB Open Calls: {result['initialCalls']}")
            print(f"Subsequent DB Open Calls (during {result['iterations']} puts): {result['subsequentCalls']}")
            print(f"Total Duration: {result['duration']:.2f} ms")
            print(f"Average Time per Put: {result['duration'] / result['iterations']:.4f} ms")
            print("-" * 30)

            if result['subsequentCalls'] > 0:
                print("FAIL: indexedDB.open was called during operations!")
                sys.exit(1)
            else:
                print("PASS: No extra DB connections opened.")

        except Exception as e:
            print(f"Error executing script: {e}")
            sys.exit(1)

        await browser.close()

if __name__ == "__main__":
    asyncio.run(run())
