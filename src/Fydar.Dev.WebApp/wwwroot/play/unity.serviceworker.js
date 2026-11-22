// Service worker for Unity web games under /play/.
// Installed using PwaInstall.razor per game.

const SCOPE = new URL(self.registration.scope).pathname.replace(/\/$/, "");
const APP_ID = SCOPE.substring(SCOPE.lastIndexOf("/") + 1);
const CACHE_NAME = `fydar-${APP_ID}`;
const MAIN_PAGE = SCOPE;
const PAGE_TIMEOUT = 4000;

function isMainPage(url) {
    return url.pathname === MAIN_PAGE || url.pathname === MAIN_PAGE + "/";
}

function isGameFile(url) {
    return url.pathname.startsWith(SCOPE + "/");
}

self.addEventListener("install", () => {
    self.skipWaiting();
});

// The page that registers this worker loaded before the worker could store it, so it is
// downloaded again here, once, which makes the game playable offline from the first visit.
async function storePage() {
    const cache = await caches.open(CACHE_NAME);
    if (await cache.match(MAIN_PAGE)) {
        return;
    }
    try {
        const response = await fetch(MAIN_PAGE);
        if (response.status === 200) {
            await cache.put(MAIN_PAGE, response);
        }
    } catch (error) {
        console.warn("Unable to store the game page for offline use:", error);
    }
}

self.addEventListener("activate", (event) => {
    event.waitUntil(Promise.all([
        self.clients.claim(),
        storePage()
    ]));
});

function timeout(milliseconds) {
    return new Promise((_, reject) => setTimeout(() => reject(new Error("Timed out")), milliseconds));
}

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET") {
        return;
    }
    const url = new URL(request.url);
    if (url.origin !== location.origin) {
        return;
    }

    if (isMainPage(url)) {
        event.respondWith((async () => {
            const network = fetch(request).then((response) => {
                // Cache only the parameterless and successful versions of the page.
                if (response.status === 200 && !url.search) {
                    const copy = response.clone();
                    event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.put(MAIN_PAGE, copy)));
                }
                return response;
            });

            // Refresh the cache once a result arrives, even if it was slow.
            event.waitUntil(network.catch(() => { }));

            try {
                const response = await Promise.race([network, timeout(PAGE_TIMEOUT)]);
                if (response.status < 500) {
                    return response;
                }
            } catch (error) {
                // Fall back to the cached page when offline, unreachable or response was too slow.
            }

            const cached = await caches.match(MAIN_PAGE, { cacheName: CACHE_NAME });
            
            // Since we have nothing cached, wait for whatever the network eventually gives.
            return cached ?? network;
        })());
    } else if (isGameFile(url)) {
        event.respondWith((async () => {
            const cache = await caches.open(CACHE_NAME);
            const cached = await cache.match(request);
            if (cached) {
                return cached;
            }

            const response = await fetch(request);
            if (response.status === 200 && response.type === "basic") {
                event.waitUntil(cache.put(request, response.clone()));
            }
            return response;
        })());
    }
});
