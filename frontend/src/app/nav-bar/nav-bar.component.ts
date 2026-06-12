import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../auth/auth.service';
import { RouterLink } from '@angular/router';
import { ConfirmDialogService } from '../shared/components/confirm-dialog/confirm-dialog.service';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent implements OnInit {
  public authService = inject(AuthService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private destroyRef = inject(DestroyRef);
  public userIsAuthenticated = false;
  public userIsAdmin = false;

  ngOnInit(): void {
    this.authService
      .getAuthStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isAuthenticated) => (this.userIsAuthenticated = isAuthenticated));

    this.authService
      .getAdminStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isAdmin) => (this.userIsAdmin = isAdmin));
  }

  requestLogout(): void {
    this.confirmDialog
      .confirm({ message: 'Are you sure you want to log out?', confirmLabel: 'Yes, log out' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => { if (confirmed) this.authService.logoutUser(); });
  }
}
