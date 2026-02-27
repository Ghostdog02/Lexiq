import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { AuthService } from './auth/auth.service';

function initializeAuth(authService: AuthService) {
  return () => authService.initializeAuthState();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideAnimationsAsync(),
    provideToastr({
      positionClass: 'toast-bottom-right',
      timeOut: 2000,
      progressBar: true,
      progressAnimation: 'decreasing',
      closeButton: true,
      preventDuplicates: true,
      countDuplicates: false,
      easing: 'ease-in',
      easeTime: 200,
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true
    }
  ],
};
