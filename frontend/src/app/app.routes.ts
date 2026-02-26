import { Routes } from '@angular/router';

import { authGuard } from './auth/guards/auth.guard';
import { noAuthGuard } from './auth/guards/no-auth.guard';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/lessons/components/home/home.component').then(m => m.HomeComponent),
        title: "Home",
        canActivate: [authGuard]
    },
    {
        path: 'google-login',
        loadComponent: () => import('./auth/google-login/google-login.component').then(m => m.GoogleLoginComponent),
        title: "Login with Google",
        canActivate: [noAuthGuard]
    },
    {
        path: "create-lesson",
        loadComponent: () => import('./features/lessons/components/lesson-editor/lesson-editor.component').then(m => m.LessonEditorComponent),
        title: "Create Lesson",
        canActivate: [authGuard]
    },
    {
        path: 'profile',
        loadComponent: () => import('./features/users/components/profile/profile.component').then(m => m.ProfileComponent),
        title: "Profile",
        canActivate: [authGuard]
    },
    {
        path: 'leaderboard',
        loadComponent: () => import('./features/users/components/leaderboard/leaderboard.component').then(m => m.LeaderboardComponent),
        title: "Leaderboard",
        canActivate: [authGuard]
    },
    {
        path: 'help',
        loadComponent: () => import('./help/help.component').then(m => m.HelpComponent),
        title: "Help"
    },
    {
        path: 'lesson/:id',
        loadComponent: () => import('./features/lessons/components/lesson-viewer/lesson-viewer.component').then(m => m.LessonViewerComponent),
        title: "Lesson Details",
        canActivate: [authGuard]
    },
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not Found"
    }
];
