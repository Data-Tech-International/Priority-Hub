window.PriorityHub = window.PriorityHub || {};

(function () {
    'use strict';

    const DB_NAME = 'PriorityHubUnlock';
    const DB_VERSION = 1;
    const STORE_NAME = 'passphraseCache';
    const RECORD_KEY = 'unlock';
    const TTL_MS = 90 * 24 * 60 * 60 * 1000; // 90 days

    // ── IndexedDB helpers ──

    function openDb() {
        return new Promise(function (resolve, reject) {
            var req = indexedDB.open(DB_NAME, DB_VERSION);
            req.onupgradeneeded = function (e) {
                e.target.result.createObjectStore(STORE_NAME);
            };
            req.onsuccess = function (e) { resolve(e.target.result); };
            req.onerror = function (e) { reject(e.target.error); };
        });
    }

    function dbPut(db, value) {
        return new Promise(function (resolve, reject) {
            var tx = db.transaction(STORE_NAME, 'readwrite');
            var store = tx.objectStore(STORE_NAME);
            var req = store.put(value, RECORD_KEY);
            req.onsuccess = function () { resolve(); };
            req.onerror = function (e) { reject(e.target.error); };
        });
    }

    function dbGet(db) {
        return new Promise(function (resolve, reject) {
            var tx = db.transaction(STORE_NAME, 'readonly');
            var store = tx.objectStore(STORE_NAME);
            var req = store.get(RECORD_KEY);
            req.onsuccess = function (e) { resolve(e.target.result); };
            req.onerror = function (e) { reject(e.target.error); };
        });
    }

    function dbDelete(db) {
        return new Promise(function (resolve, reject) {
            var tx = db.transaction(STORE_NAME, 'readwrite');
            var store = tx.objectStore(STORE_NAME);
            var req = store.delete(RECORD_KEY);
            req.onsuccess = function () { resolve(); };
            req.onerror = function (e) { reject(e.target.error); };
        });
    }

    // ── Crypto helpers ──

    function randomBytes(length) {
        var buf = new Uint8Array(length);
        crypto.getRandomValues(buf);
        return buf;
    }

    function encodeUtf8(str) {
        return new TextEncoder().encode(str);
    }

    function decodeUtf8(buf) {
        return new TextDecoder().decode(buf);
    }

    function bufToBase64(buf) {
        var bytes = buf instanceof Uint8Array ? buf : new Uint8Array(buf);
        var binary = '';
        for (var i = 0; i < bytes.length; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }

    function base64ToBuf(b64) {
        var binary = atob(b64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }

    // Generate a non-extractable AES-GCM device key.
    function generateDeviceKey() {
        return crypto.subtle.generateKey(
            { name: 'AES-GCM', length: 256 },
            false, // non-extractable
            ['encrypt', 'decrypt']
        );
    }

    // Encrypt passphrase bytes using the device key.
    async function encryptPassphrase(deviceKey, passphrase) {
        var iv = randomBytes(12);
        var ciphertext = await crypto.subtle.encrypt(
            { name: 'AES-GCM', iv: iv },
            deviceKey,
            encodeUtf8(passphrase)
        );
        return { ciphertext: new Uint8Array(ciphertext), iv: iv };
    }

    // Decrypt passphrase bytes using the device key.
    async function decryptPassphrase(deviceKey, ciphertext, iv) {
        var plaintext = await crypto.subtle.decrypt(
            { name: 'AES-GCM', iv: iv },
            deviceKey,
            ciphertext
        );
        return decodeUtf8(plaintext);
    }

    // ── Public API ──

    /**
     * Wraps the passphrase with a randomly generated AES-GCM device key and
     * persists the wrapped material plus metadata to IndexedDB.
     *
     * The device key is stored as a non-extractable CryptoKey object directly
     * in IndexedDB (structured-clone algorithm), so raw key bytes are never
     * exposed to JavaScript consumers.
     *
     * @param {string} passphrase - The passphrase to cache.
     * @returns {Promise<void>}
     */
    async function wrapAndStore(passphrase) {
        var deviceKey = await generateDeviceKey();
        var encrypted = await encryptPassphrase(deviceKey, passphrase);
        var expiresAt = Date.now() + TTL_MS;

        var record = {
            // Serialisable ciphertext and IV stored as Base64 strings so that
            // the record survives any IndexedDB structured-clone edge cases.
            ciphertext: bufToBase64(encrypted.ciphertext),
            iv: bufToBase64(encrypted.iv),
            expiresAt: expiresAt,
            // The CryptoKey is stored as a structured-clone object.
            // It is non-extractable so exportKey() on it will fail, but the
            // object itself can be stored and retrieved from IndexedDB and
            // then used for decrypt operations.
            deviceKey: deviceKey
        };

        var db = await openDb();
        await dbPut(db, record);
        db.close();
    }

    /**
     * Loads the wrapped passphrase from IndexedDB, verifies the TTL and
     * integrity, and returns the plaintext passphrase.
     *
     * Returns null when:
     *   - No cached entry exists.
     *   - The TTL has expired (the stale entry is wiped).
     *   - AES-GCM authentication tag verification fails (tamper detected;
     *     the corrupt entry is wiped).
     *
     * @returns {Promise<string|null>} The passphrase, or null.
     */
    async function loadAndUnwrap() {
        var db;
        try {
            db = await openDb();
            var record = await dbGet(db);

            if (!record) {
                db.close();
                return null;
            }

            // TTL check
            if (Date.now() > record.expiresAt) {
                await dbDelete(db);
                db.close();
                return null;
            }

            // Attempt to decrypt; AES-GCM authentication tag failure throws.
            var passphrase = await decryptPassphrase(
                record.deviceKey,
                base64ToBuf(record.ciphertext),
                base64ToBuf(record.iv)
            );

            db.close();
            return passphrase;
        } catch (err) {
            // Integrity failure or tamper detected — secure wipe.
            // Deletion errors are intentionally swallowed here: if the wipe
            // fails we still return null so the caller falls back to
            // re-prompting; leaking the error would not benefit the user.
            if (db) {
                try { await dbDelete(db); } catch (_) { }
                db.close();
            }
            return null;
        }
    }

    /**
     * Immediately removes all cached unlock material from IndexedDB.
     *
     * @returns {Promise<void>}
     */
    async function clearCache() {
        var db = await openDb();
        await dbDelete(db);
        db.close();
    }

    /**
     * Returns true if a valid (non-expired) cached entry exists.
     *
     * @returns {Promise<boolean>}
     */
    async function hasValidCache() {
        var db;
        try {
            db = await openDb();
            var record = await dbGet(db);
            db.close();
            if (!record) return false;
            return Date.now() <= record.expiresAt;
        } catch (_) {
            if (db) db.close();
            return false;
        }
    }

    window.PriorityHub.passphraseCache = {
        wrapAndStore: wrapAndStore,
        loadAndUnwrap: loadAndUnwrap,
        clearCache: clearCache,
        hasValidCache: hasValidCache
    };
}());
