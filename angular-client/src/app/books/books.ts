import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { BookApi } from './book-api';
import { Book } from './book.model';
import { isbn13Validator } from './isbn.validator';
import { CreateBook } from './create-book.model';
import { CategoryApi } from '../categories/category-api';
import { Category } from '../categories/category.model';

@Component({
  selector: 'app-books',
  imports: [ReactiveFormsModule],
  templateUrl: './books.html',
  styleUrl: './books.scss',
})
export class Books {
  private readonly bookApi = inject(BookApi);
  private readonly categoryApi = inject(CategoryApi);
  private readonly formBuilder = inject(FormBuilder);

  private readonly maxYear = new Date().getFullYear() + 1;

  protected readonly books = signal<Book[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly isCreating = signal(false);

  protected readonly categoriesById = computed(() => {
    const map = new Map<number, string>();
    for (const category of this.categories()) {
      map.set(category.id, category.name);
    }
    return map;
  });

  protected readonly form = this.formBuilder.group({
    title: ['', [Validators.required, Validators.maxLength(255)]],
    isbn: ['', [Validators.required, Validators.pattern(/^\d{13}$/), isbn13Validator]],
    publicationYear: [null as number | null, [Validators.required, Validators.min(1450), Validators.max(this.maxYear)]],
    categoryId: [null as number | null, [Validators.required]],
    synopsis: [''],
  });

  protected readonly isbnControl = this.form.controls.isbn;



  constructor() {
    this.loadBooks();
    this.loadCategories();
  }

  private loadBooks(): void {
    this.bookApi.getAll().subscribe({
      next: (books) => this.books.set(books),
    });
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

    const value = this.form.getRawValue();
    const request: CreateBook = {
      title: value.title!,
      isbn: value.isbn!,
      publicationYear: value.publicationYear!,
      categoryId: value.categoryId!,
      synopsis: value.synopsis?.trim() ? value.synopsis : null,
    };

    this.bookApi.create(request).subscribe({
      next: () => {
        this.form.reset();
        this.isCreating.set(false);
        this.loadBooks();
      },
      error: (err) => {
        this.isCreating.set(false);
        if (err.status === 403) {
          this.error.set('You do not have permission to create books (requires Librarian or Admin).');
        } else if (err.status === 409) {
          this.error.set('A book with that ISBN already exists.');
        } else {
          this.error.set('Could not create the book.');
        }
      },
    });
  }
}
