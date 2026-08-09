import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CategoryApi } from './category-api';
import { Category } from './category.model';
import { CategoryDetail } from './category-detail/category-detail';

@Component({
  selector: 'app-categories',
    imports: [ReactiveFormsModule, CategoryDetail],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class Categories {
  private readonly categoryApi = inject(CategoryApi);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly categories = signal<Category[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly isCreating = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required]],
    description: [''],
  });

  protected readonly selectedCategory = signal<Category | null>(null);

  openDetail(category: Category): void {
    this.selectedCategory.set(category);
  }

  closeDetail(): void {
    this.selectedCategory.set(null);
  }

  refreshAndClose(): void {
    this.closeDetail();
    this.loadCategories();
  }

  constructor() {
    this.loadCategories();
  }

  private loadCategories(): void {
    this.categoryApi.getAll().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.error.set(null);
    this.isCreating.set(true);

    const { name, description } = this.form.getRawValue();

    this.categoryApi.create({ name, description: description || null }).subscribe({
      next: () => {
        this.form.reset();
        this.isCreating.set(false);
        this.loadCategories();
      },
      error: (err) => {
        this.isCreating.set(false);
        if (err.status === 403) {
          this.error.set('You do not have permission to create categories (requires Librarian or Admin).');
        } else if (err.status === 409) {
          this.error.set('A category with that name already exists.');
        } else {
          this.error.set('Could not create the category.');
        }
      },
    });
  }
}
