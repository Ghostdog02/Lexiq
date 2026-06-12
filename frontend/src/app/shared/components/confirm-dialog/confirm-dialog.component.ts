import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActiveDialog, ConfirmDialogService } from './confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent implements OnInit {
  private readonly service = inject(ConfirmDialogService);
  private readonly destroyRef = inject(DestroyRef);

  active: ActiveDialog | null = null;

  ngOnInit(): void {
    this.service.active$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(dialog => (this.active = dialog));
  }

  respond(result: boolean): void {
    this.active?.resolve(result);
  }
}
