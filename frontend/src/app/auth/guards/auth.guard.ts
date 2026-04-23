import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { ToastrService } from 'ngx-toastr';

import { AuthService } from '../auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastr = inject(ToastrService);

  if (authService.getIsAuth()) {
    return true;
  }

  toastr.info('Sign in to continue', 'Access restricted', { toastClass: 'ngx-toastr toast-auth' });

  return router.createUrlTree(['/google-login'], {
    queryParams: { returnUrl: state.url }
  });
};
