import { dotnet } from './_framework/dotnet.js'

const dotnetRuntime = await dotnet
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
