import { Component, input, output, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Book } from '../book.model';
import { Category } from '../../categories/category.model';
import { BookApi } from '../book-api';
import { UpdateBook } from '../update-book.model';

@Component({
  selector: 'app-book-detail',
  imports: [FormsModule],
  templateUrl: './book-detail.html',
  styleUrl: './book-detail.scss',
})
export class BookDetail implements OnInit {
  private readonly bookApi = inject(BookApi);

  readonly book = input.required<Book>();
  readonly categories = input.required<Category[]>();
  readonly close = output<void>();
  readonly saved = output<void>();

  protected readonly error = signal<string | null>(null);
  protected readonly isSaving = signal(false);

  protected editable = {
    title: '',
    isbn: '',
    publicationYear: null as number | null,
    categoryId: null as number | null,
    synopsis: '' as string | null,
  };

  protected readonly categoriesById = computed(() => {
    const map = new Map<number, string>();
    for (const category of this.categories()) {
      map.set(category.id, category.name);
    }
    return map;
  });

  ngOnInit(): void {
    const b = this.book();
    this.editable = {
      title: b.title,
      isbn: b.isbn,
      publicationYear: b.publicationYear,
      categoryId: b.categoryId,
      synopsis: b.synopsis,
    };
  }

  save(): void {
    this.error.set(null);
    this.isSaving.set(true);

    const request: UpdateBook = {
      title: this.editable.title,
      isbn: this.editable.isbn,
      publicationYear: this.editable.publicationYear!,
      categoryId: this.editable.categoryId!,
      synopsis: this.editable.synopsis?.trim() ? this.editable.synopsis : null,
    };

    this.bookApi.update(this.book().id, request).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        if (err.status === 403) {
          this.error.set('You do not have permission to edit books.');
        } else if (err.status === 409) {
          this.error.set('A book with that ISBN already exists.');
        } else if (err.status === 400) {
          this.error.set('Check the fields: ISBN must be a valid 13-digit ISBN-13 and the year must be valid.');
        } else {
          this.error.set('Could not save the changes.');
        }
      },
    });
  }
}
