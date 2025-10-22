import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';

export const routes: Routes = [
    {
        path: '',
        component: HomeComponent,
        title: "Home"
    },
    {
        path: "register",
        loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent),
        title: "Register"
    },
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not-Found",
    },
];

