import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Categories } from './categories/categories';
import { Login } from './auth/login/login';
import { Register } from './auth/register/register';
import { authGuard } from './auth/auth-guard';
import { Books } from './books/books';
import { Loans } from './loans/loans';
import { staffGuard } from './auth/staff-guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'categories', component: Categories, canActivate: [authGuard] },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'books', component: Books, canActivate: [authGuard] },
  { path: 'loans', component: Loans, canActivate: [authGuard, staffGuard] }
];
