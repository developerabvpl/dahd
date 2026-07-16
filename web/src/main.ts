import { isDevMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// A production build once registered the PWA service worker on this origin;
// in dev it would keep serving that stale cached bundle (old UI, missing
// forms). Unregister any leftover workers and their caches before bootstrap.
if (isDevMode() && 'serviceWorker' in navigator) {
  navigator.serviceWorker.getRegistrations()
    .then(regs => Promise.all(regs.map(r => r.unregister())))
    .then(unregistered => {
      if (unregistered.some(Boolean) && 'caches' in window) {
        return caches.keys().then(keys =>
          Promise.all(keys.filter(k => k.startsWith('ngsw')).map(k => caches.delete(k)))
        ).then(() => location.reload());
      }
      return undefined;
    })
    .catch(() => { /* non-fatal */ });
}

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
