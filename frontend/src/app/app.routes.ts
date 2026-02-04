import { Routes } from '@angular/router';
import { HomeComponent } from './features/lessons/components/home/home.component';
import { GoogleLoginComponent } from './auth/google-login/google-login.component';
import { ProfileComponent } from './features/users/components/profile/profile.component';
import { LeaderboardComponent } from './features/users/components/leaderboard/leaderboard.component';
import { HelpComponent } from './help/help.component';

export const routes: Routes = [
    {
        path: '',
        component: HomeComponent,
        title: "Home"
    },
    {
        path: 'google-login',
        component: GoogleLoginComponent,
        title: "Login with Google"
    },
    {
        path: "create-lesson",
        loadComponent: () => import('./features/lessons/components/lesson-editor/lesson-editor.component').then(m => m.LessonEditorComponent),
        title: "Create Lesson"
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
        path: 'lesson/:id',
        loadComponent: () => import('./features/lessons/components/lesson-viewer/lesson-viewer.component').then(m => m.LessonViewerComponent),
        title: "Lesson Details"
    },
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not Found"
    }
];
