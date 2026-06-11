import { ErrorHandler, inject, Injectable, Injector } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';

const TOAST_OPTIONS = { toastClass: 'ngx-toastr toast-auth' };

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private injector = inject(Injector);

  handleError(error: unknown): void {
    // HTTP errors are already handled by the auth interceptor — skip
    if (error instanceof HttpErrorResponse) {
      return;
    }

    console.error('[GlobalErrorHandler]', error);

    try {
      const toastr = this.injector.get(ToastrService);
      toastr.error('Something went wrong. Please try again.', 'Error', TOAST_OPTIONS);
    } catch {
      // Toastr not yet available during app boot — nothing to do
    }
  }
}
