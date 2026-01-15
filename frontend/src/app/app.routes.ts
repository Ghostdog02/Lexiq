import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GoogleLoginComponent } from './auth/google-login/google-login.component';

export const routes: Routes = [
    {
        path: '',
        component: HomeComponent,
        title: "Home"
    },
    {
        path:'google-login',
        component: GoogleLoginComponent,
        title: "Login with Google"
    },
    {
        path: "exercise/create",
        loadComponent: () => import('./create-exercise/create-exercise').then(m => m.CreateLesson),
        title: "Create Exercise"
    }, 
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not Found",
    },  
];

