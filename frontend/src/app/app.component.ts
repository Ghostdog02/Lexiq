import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { trigger, transition, query, style, animate, group } from '@angular/animations';
import { NavBarComponent } from './nav-bar/nav-bar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavBarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  animations: [
    trigger('pageTransition', [
      transition('* <=> *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(28px)' }),
        ], { optional: true }),
        group([
          query(':leave', [
            style({ position: 'absolute', top: 0, left: 0, width: '100%' }),
            animate('420ms cubic-bezier(0.4, 0, 1, 1)',
              style({ opacity: 0, transform: 'translateY(16px)' }))
          ], { optional: true }),
          query(':enter', [
            animate('750ms cubic-bezier(0.0, 0.0, 0.2, 1)',
              style({ opacity: 1, transform: 'translateY(0)' }))
          ], { optional: true }),
        ]),
      ])
    ])
  ]
})
export class AppComponent {
  getRouteState(outlet: RouterOutlet): string {
    return outlet.isActivated
      ? outlet.activatedRoute.snapshot.url.join('/') || 'home'
      : '';
  }
}
