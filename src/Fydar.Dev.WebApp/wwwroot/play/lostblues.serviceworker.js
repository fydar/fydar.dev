const CACHE_NAME = "fydar-lostblues-v0.1.1";
const MAIN_PAGE = "/play/lostblues";
const ASSETS = [
    MAIN_PAGE,
    "/play/lostblues/favicon.svg",
    "/play/lostblues/manifest.webmanifest",
    "/play/lostblues/build/lostblues.loader.js",
    "/play/lostblues/build/lostblues.framework.js.br",
];

self.addEventListener('install', (e) => {
    self.skipWaiting();
    e.waitUntil(
        caches.open(CACHE_NAME).then(async (cache) => {
            const promises = ASSETS.map(async (url) => {
                const existingResponse = await cache.match(url);
                if (!existingResponse) {
                    try {
                        const response = await fetch(url);
                        if (response.ok) {
                            return cache.put(url, response);
                        }
                    } catch (error) {
                        console.error(`Failed to cache ${url}:`, error);
                    }
                }
                return Promise.resolve();
            });
            return Promise.all(promises);
        })
    );
});

self.addEventListener('activate', (e) => {
    e.waitUntil(
        Promise.all([
            self.clients.claim(),
            caches.keys().then((keys) => {
                return Promise.all(
                    keys.map((key) => {
                        if (key.startsWith("fydar-lostblues-") && key !== CACHE_NAME) {
                            return caches.delete(key);
                        }
                    })
                );
            })
        ])
    );
});

self.addEventListener('fetch', (e) => {
    const url = new URL(e.request.url);
    const isMainPage = url.pathname === MAIN_PAGE || url.pathname === MAIN_PAGE + "/";

    if (!isMainPage && !url.pathname.startsWith(MAIN_PAGE + "/")) return;

    if (isMainPage) {
        e.respondWith(
            fetch(MAIN_PAGE)
                .then((networkResponse) => {
                    caches.open(CACHE_NAME).then((cache) => cache.put(MAIN_PAGE, networkResponse.clone())).catch(() => { });
                    return networkResponse;
                })
                .catch(async () => {
                    const cached = await caches.match(MAIN_PAGE);
                    return cached || Response.error();
                })
        );
    }
    else {
        e.respondWith(
            caches.match(e.request).then((cachedResponse) => {
                if (cachedResponse) return cachedResponse;

                return fetch(e.request).then((networkResponse) => {
                    if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== 'basic') {
                        return networkResponse;
                    }

                    const responseToCache = networkResponse.clone();
                    caches.open(CACHE_NAME).then((cache) => cache.put(e.request, responseToCache)).catch(() => { });
                    return networkResponse;
                }).catch(() => Response.error());
            })
        );
    }
});
