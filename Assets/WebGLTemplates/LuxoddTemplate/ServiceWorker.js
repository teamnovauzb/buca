// Cleanup worker for browsers that installed the cache-first worker from an
// older Buca build. New builds do not register a service worker because the
// Luxodd arcade host already controls delivery and versioning.
const legacyCachePrefix = "DefaultCompany-RealBuca-";

async function clearLegacyBucaCaches() {
  const cacheNames = await caches.keys();
  await Promise.all(
    cacheNames
      .filter((name) => name.startsWith(legacyCachePrefix))
      .map((name) => caches.delete(name))
  );
}

self.addEventListener("install", (event) => {
  event.waitUntil(Promise.all([self.skipWaiting(), clearLegacyBucaCaches()]));
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    clearLegacyBucaCaches()
      .then(() => self.clients.claim())
      .then(() => self.registration.unregister())
  );
});

// If this cleanup worker briefly controls an existing tab, always prefer the
// network and use an old cached response only when the network is unavailable.
self.addEventListener("fetch", (event) => {
  event.respondWith(
    fetch(event.request, { cache: "no-store" })
      .catch(() => caches.match(event.request))
  );
});
