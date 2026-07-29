const CACHE_PREFIX = "fydar-astralelites-";
const CACHE_NAME = CACHE_PREFIX + "v0.1.2.12";
const MAIN_PAGE = "/play/astralelites";
const ASSETS = [
    MAIN_PAGE,
    "/play/astralelites/favicon.svg",
    "/play/astralelites/manifest.webmanifest",
    "/play/astralelites/build/0.1.2.12.loader.js",
    "/play/astralelites/build/0.1.2.12.framework.js.br",
    "/play/astralelites/build/0.1.2.12.wasm.br",
    "/play/astralelites/build/0.1.2.12.data.br",
    "/play/astralelites/StreamingAssets/AssetBundles/music.data",
    "/play/astralelites/StreamingAssets/AssetBundles/music.data.manifest",
    "/play/astralelites/StreamingAssets/AssetBundles/sfx.data",
    "/play/astralelites/StreamingAssets/AssetBundles/sfx.data.manifest"
];

self.addEventListener('install', (e) => {
    console.debug(`[SW] install: opening cache '${CACHE_NAME}'`);
    self.skipWaiting();
    e.waitUntil(
        caches.open(CACHE_NAME).then(async (cache) => {
            console.debug(`[SW] install: cache opened, pre-caching ${ASSETS.length} assets`);
            const promises = ASSETS.map(async (url) => {
                const existingResponse = await cache.match(url);
                if (existingResponse) {
                    console.debug(`[SW] install: cache hit (skipping fetch) ${url}`);
                    return Promise.resolve();
                }
                console.debug(`[SW] install: cache miss, fetching ${url}`);
                try {
                    const response = await fetch(url);
                    if (response.ok) {
                        return cache.put(url, response).then(() => {
                            console.debug(`[SW] install: cached ${url} (${response.status})`);
                        });
                    } else {
                        console.error(`[SW] install: fetch not ok for ${url} — status ${response.status}`);
                    }
                } catch (error) {
                    console.error(`[SW] install: fetch threw for ${url}:`, error);
                }
                return Promise.resolve();
            });
            return Promise.all(promises).then(() => {
                console.debug(`[SW] install: pre-cache complete`);
            });
        })
    );
});

self.addEventListener('activate', (e) => {
    console.debug(`[SW] activate: claiming clients and pruning old caches`);
    e.waitUntil(
        Promise.all([
            self.clients.claim().then(() => {
                console.debug(`[SW] activate: clients claimed`);
            }),
            caches.keys().then((keys) => {
                const oldCaches = keys.filter(key =>
                    key.startsWith(CACHE_PREFIX) && key !== CACHE_NAME
                );
                console.debug(`[SW] activate: deleting ${oldCaches.length} old cache(s):`, oldCaches);
                return Promise.all(oldCaches.map(key =>
                    caches.delete(key).then(() => {
                        console.debug(`[SW] activate: deleted old cache '${key}'`);
                    })
                ));
            })
        ]).then(() => {
            console.debug(`[SW] activate: complete`);
        })
    );
});

self.addEventListener('fetch', (e) => {
    if (e.request.method !== 'GET') return;
    const url = new URL(e.request.url);
    if (url.origin !== location.origin) return;
    const isMainPage = url.pathname === MAIN_PAGE || url.pathname === MAIN_PAGE + "/";
 
    if (!isMainPage && !url.pathname.startsWith(MAIN_PAGE + "/")) return;

    if (isMainPage) {
        console.debug(`[SW] fetch: network-first for main page ${url.pathname}`);
        e.respondWith(
            fetch(MAIN_PAGE)
                .then((networkResponse) => {
                    console.debug(`[SW] fetch: main page network response ${networkResponse.status}, updating cache`);
                    const clonedResponse = networkResponse.clone();
                    caches.open(CACHE_NAME)
                        .then((cache) => cache.put(MAIN_PAGE, clonedResponse).then(() => {
                            console.debug(`[SW] fetch: main page cache updated`);
                        }))
                        .catch((error) => { console.error(`[SW] fetch: failed to update main page cache:`, error); });
                    return networkResponse;
                })
                .catch(async (error) => {
                    console.error(`[SW] fetch: network failed for main page, falling back to cache:`, error);
                    const cached = await caches.match(MAIN_PAGE);
                    if (cached) {
                        console.debug(`[SW] fetch: serving main page from cache`);
                    } else {
                        console.error(`[SW] fetch: no cached main page available, returning error response`);
                    }
                    return cached || Response.error();
                })
        );
    }
    else {
        console.debug(`[SW] fetch: cache-first for asset ${url.pathname}`);
        e.respondWith(
            caches.match(e.request).then((cachedResponse) => {
                if (cachedResponse) {
                    console.debug(`[SW] fetch: cache hit for ${url.pathname}`);
                    return cachedResponse;
                }

                console.debug(`[SW] fetch: cache miss, fetching from network ${url.pathname}`);
                return fetch(e.request).then((networkResponse) => {
                    if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== 'basic') {
                        console.error(`[SW] fetch: unexpected network response for ${url.pathname} — status ${networkResponse?.status}, type ${networkResponse?.type}`);
                        return networkResponse;
                    }

                    console.debug(`[SW] fetch: network response ok for ${url.pathname}, storing in cache`);
                    const responseToCache = networkResponse.clone();
                    caches.open(CACHE_NAME)
                        .then((cache) => cache.put(e.request, responseToCache).then(() => {
                            console.debug(`[SW] fetch: cached ${url.pathname}`);
                        }))
                        .catch((error) => { console.error(`[SW] fetch: failed to cache ${url.pathname}:`, error); });
                    return networkResponse;
                }).catch((error) => {
                    console.error(`[SW] fetch: network fetch threw for ${url.pathname}:`, error);
                    return Response.error();
                });
            })
        );
    }
});
