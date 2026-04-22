import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { AuthService } from './auth.service';

const TOAST_OPTIONS = { toastClass: 'ngx-toastr toast-auth' };

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastr = inject(ToastrService);

  const credentialReq = req.clone({ withCredentials: true });

  return next(credentialReq).pipe(
    catchError(error => {
      switch (error.status) {
        case 401:
          authService.clearAuthState();
          router.navigate(['/google-login'], {
            queryParams: { returnUrl: router.url },
          });
          break;

        case 403:
          toastr.error(
            "You don't have permission to do that",
            'Forbidden',
            TOAST_OPTIONS,
          );
          break;

        case 0:
          toastr.error(
            'Unable to reach the server',
            'Connection error',
            TOAST_OPTIONS,
          );
          break;

        default:
          if (error.status >= 500) {
            toastr.error(
              'Something went wrong. Please try again later',
              'Server error',
              TOAST_OPTIONS,
            );
          }
          break;
      }

      return throwError(() => error);
    })
  );
};
