import { AfterViewInit, Component, inject, OnDestroy } from '@angular/core';
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
export class GoogleLoginComponent implements AfterViewInit, OnDestroy {
  private authService = inject(AuthService);
  private checkInterval: any;

  ngAfterViewInit(): void {
    this.loadGoogleSignIn();
  }

  ngOnDestroy(): void {
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
    }
  }

  private loadGoogleSignIn(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      setTimeout(() => {
        this.initializeGoogleSignIn();
      }, 100);
      return;
    }

    // If not loaded, poll until it's available
    let attempts = 0;
    const maxAttempts = 50; // 5 seconds max

    this.checkInterval = setInterval(() => {
      attempts++;

      if (typeof google !== 'undefined' && google.accounts) {
        clearInterval(this.checkInterval);
        this.initializeGoogleSignIn();
      } else if (attempts >= maxAttempts) {
        clearInterval(this.checkInterval);
        console.error('Google Sign-In script failed to load');
      }
    }, 100);
  }

  private initializeGoogleSignIn(): void {
    const clientId = import.meta.env.NG_GOOGLE_CLIENT_ID;

    if (!clientId) {
      console.error('Google Client ID is missing! Check your .env file.');
      return;
    }

    const buttonElement = document.getElementById('g_id_onload');
    if (!buttonElement) {
      console.error('Google button container element not found!');
      return;
    }

    try {
      google.accounts.id.initialize({
        client_id: clientId,
        callback: (response: any) => this.handleCredentialResponse(response),
        ux_mode: 'popup'
      });

      google.accounts.id.renderButton(
        buttonElement,
        {
          theme: 'filled_black',
          size: 'large',
          shape: 'pill',
          width: 320,
          type: 'standard',
          text: 'signin_with',
          logo_alignment: 'center'
        }
      );

      google.accounts.id.prompt();
    } catch (error) {
      console.error('Error initializing Google Sign-In:', error);
    }
  }

  handleCredentialResponse(response: any) {
    this.authService.loginUserWithGoogle(response.credential);
  }
}