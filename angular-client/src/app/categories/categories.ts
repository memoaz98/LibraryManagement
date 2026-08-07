import { Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { CategoryApi } from './category-api';

@Component({
  selector: 'app-categories',
  imports: [AsyncPipe],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class Categories {
  private readonly categoryApi = inject(CategoryApi);
  readonly categories$ = this.categoryApi.getAll();
}
