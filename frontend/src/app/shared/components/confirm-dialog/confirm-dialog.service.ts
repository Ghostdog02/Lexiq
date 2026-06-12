import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export interface ConfirmConfig {
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

export interface ActiveDialog {
  config: ConfirmConfig;
  resolve: (result: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly _active$ = new Subject<ActiveDialog | null>();
  readonly active$ = this._active$.asObservable();

  confirm(config: ConfirmConfig): Observable<boolean> {
    return new Observable(observer => {
      this._active$.next({
        config,
        resolve: (result: boolean) => {
          this._active$.next(null);
          observer.next(result);
          observer.complete();
        },
      });
    });
  }
}
