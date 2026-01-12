import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { GoogleLoginComponent } from './auth/google-login/google-login.component';
import { CoursesComponent } from './courses/courses.component';

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
      path:'courses',
      component: CoursesComponent,
      title: "Courses"  
    },
    {
        path: '**',
        loadComponent: () => import('./not-found/not-found.component').then(m => m.NotFoundComponent),
        title: "Not-Found",
    },
];

