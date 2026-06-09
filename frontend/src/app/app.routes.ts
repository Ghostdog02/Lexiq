import { Routes } from '@angular/router';

import { authGuard } from './auth/guards/auth.guard';
import { noAuthGuard } from './auth/guards/no-auth.guard';
import { contentGuard } from './auth/guards/content.guard';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./about/about-page.component').then(m => m.AboutPageComponent),
        title: "Home"
    },
    {
        path: 'courses',
        loadComponent: () => import('./features/lessons/components/home/home.component').then(m => m.HomeComponent),
        title: "Courses",
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
        canActivate: [authGuard, contentGuard]
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
        path: 'about',
        loadComponent: () => import('./about/about-page.component').then(m => m.AboutPageComponent),
        title: "About Lexiq"
    },
    {
        path: 'help',
        loadComponent: () => import('./help/help.component').then(m => m.HelpComponent),
        title: "Help"
    },
    {
        path: 'terms',
        loadComponent: () => import('./terms/terms.component').then(m => m.TermsComponent),
        title: "Terms of Service"
    },
    {
        path: 'privacy',
        loadComponent: () => import('./privacy/privacy.component').then(m => m.PrivacyComponent),
        title: "Privacy Policy"
    },
    {
        path: 'lesson/:id',
        loadComponent: () => import('./features/lessons/components/lesson-viewer/lesson-viewer.component').then(m => m.LessonViewerComponent),
        title: "Lesson Details",
        canActivate: [authGuard]
    },
    {
        path: 'lesson/:id/exercises',
        loadComponent: () => import('./features/lessons/components/exercise-viewer/exercise-viewer.component').then(m => m.ExerciseViewerComponent),
        title: "Exercises",
        canActivate: [authGuard]
    },
    {
        path: 'error',
        loadComponent: () => import('./error-page/error-page.component').then(m => m.ErrorPageComponent),
        title: "Error"
    },
    {
        path: '**',
        loadComponent: () => import('./error-page/error-page.component').then(m => m.ErrorPageComponent),
        title: "Not Found"
    }
];
