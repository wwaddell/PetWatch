const dbName = 'PetSitterDB';
const dbVersion = 2; // Incremented version for schema update

export function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);

        request.onerror = (event) => {
            reject('Error opening database');
        };

        request.onsuccess = (event) => {
            resolve('Database opened successfully');
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
        };
    });
}

export function putRecord(storeName, record) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);
        request.onsuccess = (event) => {
            const db = event.target.result;
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);
            const putRequest = store.put(record);

            putRequest.onsuccess = () => resolve();
            putRequest.onerror = (e) => reject(e.target.error);
        };
        request.onerror = (e) => reject(e.target.error);
    });
}

export function getAllRecords(storeName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);
        request.onsuccess = (event) => {
            const db = event.target.result;
            const transaction = db.transaction([storeName], 'readonly');
            const store = transaction.objectStore(storeName);
            const getAllRequest = store.getAll();

            getAllRequest.onsuccess = () => resolve(getAllRequest.result);
            getAllRequest.onerror = (e) => reject(e.target.error);
        };
        request.onerror = (e) => reject(e.target.error);
    });
}

export function deleteRecord(storeName, id) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, dbVersion);
        request.onsuccess = (event) => {
            const db = event.target.result;
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);
            const deleteRequest = store.delete(id);

            deleteRequest.onsuccess = () => resolve();
            deleteRequest.onerror = (e) => reject(e.target.error);
        };
        request.onerror = (e) => reject(e.target.error);
    });
}
