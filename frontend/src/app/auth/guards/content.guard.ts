import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { ToastrService } from 'ngx-toastr';

import { AuthService } from '../auth.service';

export const contentGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastr = inject(ToastrService);

  if (authService.getIsAdmin() || authService.getIsContentCreator()) {
    return true;
  }

  toastr.info('You do not have permission to create lessons', 'Access restricted', {
    toastClass: 'ngx-toastr toast-auth'
  });

  return router.createUrlTree(['/']);
};
