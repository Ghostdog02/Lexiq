import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { Subscription } from 'rxjs';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent implements OnInit {
  public authService = inject(AuthService);
  private authListenerSubs: Subscription = new Subscription();
  private adminListenerSubs: Subscription = new Subscription();
  public userIsAuthenticated: boolean = false;
  public userIsAdmin: boolean = false;

  ngOnInit(): void {
    this.authListenerSubs = this.authService
      .getAuthStatusListener()
      .subscribe((isAuthenticated) => (this.userIsAuthenticated = isAuthenticated));

    this.adminListenerSubs = this.authService
      .getAdminStatusListener()
      .subscribe((isAdmin) => (this.userIsAdmin = isAdmin));
  }
}
