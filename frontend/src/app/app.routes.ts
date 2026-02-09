import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/lessons/components/home/home.component').then(m => m.HomeComponent),
        title: "Home"
    },
    {
        path: 'google-login',
        loadComponent: () => import('./auth/google-login/google-login.component').then(m => m.GoogleLoginComponent),
        title: "Login with Google"
    },
    {
        path: "create-lesson",
        loadComponent: () => import('./features/lessons/components/lesson-editor/lesson-editor.component').then(m => m.LessonEditorComponent),
        title: "Create Lesson"
    },
    {
        path: 'profile',
        loadComponent: () => import('./features/users/components/profile/profile.component').then(m => m.ProfileComponent),
        title: "Profile"
    },
    {
        path: 'leaderboard',
        loadComponent: () => import('./features/users/components/leaderboard/leaderboard.component').then(m => m.LeaderboardComponent),
        title: "Leaderboard"
    },
    {
        path: 'help',
        loadComponent: () => import('./help/help.component').then(m => m.HelpComponent),
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
