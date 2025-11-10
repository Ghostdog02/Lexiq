import { AfterViewInit, Component, inject } from '@angular/core';
import { AuthService } from '../auth.service';

declare const google: any;

@Component({
  selector: 'app-google-login',
  imports: [],
  standalone: true,
  providers: [],
  templateUrl: './google-login.component.html',
  styleUrl: './google-login.component.scss',
})
export class GoogleLoginComponent implements AfterViewInit {
  authService = inject(AuthService);
  ngAfterViewInit(): void {
    google.accounts.id.initialize({
        client_id: import.meta.env['GOOGLE_CLIENT_ID'],
      callback: (response: any) => this.handleCredentialResponse(response),
    });

    google.accounts.id.renderButton(
      document.getElementById('g_id_onload'),
      { 
        theme: 'filled_black', 
        size: 'large', 
        shape: 'pill',
        width: 320
      }
    );

    google.accounts.id.prompt();
  }

  handleCredentialResponse(response: any) {
    console.log('Google credential:', response.credential);

    this.authService.loginUserWithGoogle(response.credential);
  }
}
