const dbName = 'PetSitterDB';
const dbVersion = 3; // Incremented version for schema update

let dbPromise = null;

function getDb() {
    if (!dbPromise) {
        dbPromise = new Promise((resolve, reject) => {
            // Singleton pattern: Connection is opened once and reused.
            // This prevents opening multiple connections which is inefficient and can lead to blocking.
            const request = indexedDB.open(dbName, dbVersion);

            request.onerror = (event) => {
                dbPromise = null;
                reject('Error opening database');
            };

            request.onsuccess = (event) => {
                const db = event.target.result;
                db.onclose = () => {
                    dbPromise = null;
                };
                resolve(db);
            };

            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                if (!db.objectStoreNames.contains('customers')) {
                    db.createObjectStore('customers', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('pets')) {
                    db.createObjectStore('pets', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('appointments')) {
                    db.createObjectStore('appointments', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('payments')) {
                    db.createObjectStore('payments', { keyPath: 'id' });
                }
                if (!db.objectStoreNames.contains('services')) {
                    db.createObjectStore('services', { keyPath: 'id' });
                }
            };
        });
    }
    return dbPromise;
}

export function openDb() {
    return getDb().then(() => 'Database opened successfully');
}

export function putRecord(storeName, record) {
    return getDb().then(db => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);

            // We use transaction.oncomplete to ensure the data is durably committed
            transaction.oncomplete = () => resolve();
            transaction.onerror = (e) => reject(e.target.error);

            store.put(record);
        });
    });
}

export function getAllRecords(storeName) {
    return getDb().then(db => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], 'readonly');
            const store = transaction.objectStore(storeName);
            const getAllRequest = store.getAll();

            getAllRequest.onsuccess = () => resolve(getAllRequest.result);
            getAllRequest.onerror = (e) => reject(e.target.error);
        });
    });
}

export function deleteRecord(storeName, id) {
    return getDb().then(db => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);

            transaction.oncomplete = () => resolve();
            transaction.onerror = (e) => reject(e.target.error);

            store.delete(id);
        });
    });
}

export function putRecords(storeName, records) {
    return getDb().then(db => {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);

            transaction.oncomplete = () => resolve();
            transaction.onerror = (e) => reject(e.target.error);

            for (const record of records) {
                store.put(record);
            }
        });
    });
}
