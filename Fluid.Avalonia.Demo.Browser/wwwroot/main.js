import { dotnet } from './_framework/dotnet.js'

// disableIntegrityCheck: skip the boot-resource SRI hashes. The runtime assets are content-
// fingerprinted, but a returning visitor can hold _framework files cached from a previous deploy
// whose bytes no longer match the new boot manifest's inlined SHA-256 — which the loader otherwise
// rejects (SRI), leaving the demo stuck on the splash. GitHub Pages (Fastly) gives no Cache-Control
// header control to flush that cache, so we drop the integrity gate instead. Safe for a public,
// read-only demo; the cost is no byte-tamper detection on the assets. Browser head only.
const dotnetRuntime = await dotnet
    .withConfig({ disableIntegrityCheck: true })
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

// Fade the loading splash out once the runtime is ready to render, then remove it.
const splash = document.getElementById('loading');
if (splash) {
    splash.classList.add('hide');
    splash.addEventListener('transitionend', () => splash.remove(), { once: true });
    // Fallback in case the transition doesn't fire (e.g. reduced motion).
    setTimeout(() => splash.remove(), 700);
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
