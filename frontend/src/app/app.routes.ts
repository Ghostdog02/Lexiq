import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GoogleLoginComponent } from './auth/google-login/google-login.component';
import { ProfileComponent } from './profile/profile.component';
import { LeaderboardComponent } from './leaderboard/leaderboard.component';
import { HelpComponent } from './help/help.component';

export const routes: Routes = [
    {
        path: '',
        component: HomeComponent,
        title: "Home"
    },
    {
        path:'google-login',
        component: GoogleLoginComponent,
        title: "Google-Login"
    },
    {
        path: 'profile',
        component: ProfileComponent,
        title: "Profile"
    },
    {
        path: 'leaderboard',
        component: LeaderboardComponent,
        title: "Leaderboard"
    },
    {
        path: 'help',
        component: HelpComponent,
        title: "Help"
    },
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not-Found",
    },
];

