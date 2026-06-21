const CACHE_NAME = "fydar-astralswarm-v0.1.0";
const MAIN_PAGE = "/play/astralswarm";
const ASSETS = [
    MAIN_PAGE,
    "/play/astralswarm/favicon.svg",
    "/play/astralswarm/manifest.webmanifest",
    "/play/astralswarm/build/astralswarm.loader.js",
    "/play/astralswarm/build/astralswarm.framework.js.br",
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
                        if (key.startsWith("fydar-astralswarm-") && key !== CACHE_NAME) {
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
    const isMainPage = url.pathname.endsWith(MAIN_PAGE) || url.pathname.endsWith(MAIN_PAGE + "/");
    const isAsset = ASSETS.some(path => url.pathname.endsWith(path));

    if (!isAsset && !isMainPage) return;

    if (isMainPage) {
        e.respondWith(
            fetch(e.request)
                .then((networkResponse) => {
                    const responseToCache = networkResponse.clone();
                    caches.open(CACHE_NAME).then((cache) => cache.put(e.request, responseToCache));
                    return networkResponse;
                })
                .catch(() => caches.match(e.request))
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
                    caches.open(CACHE_NAME).then((cache) => cache.put(e.request, responseToCache));
                    return networkResponse;
                });
            })
        );
    }
});
