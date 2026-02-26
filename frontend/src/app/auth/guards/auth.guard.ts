import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getIsAuth()) {
    return true;
  }

  return router.createUrlTree(['/google-login'], {
    queryParams: { returnUrl: state.url }
  });
};
