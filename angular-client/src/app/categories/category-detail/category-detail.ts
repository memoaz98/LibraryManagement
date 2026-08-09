import { Component, inject, input, output, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Category } from '../category.model';
import { CategoryApi } from '../category-api';

@Component({
  selector: 'app-category-detail',
  imports: [ReactiveFormsModule],
  templateUrl: './category-detail.html',
  styleUrl: './category-detail.scss',
})
export class CategoryDetail implements OnInit {
  private readonly categoryApi = inject(CategoryApi);
  private readonly formBuilder = inject(FormBuilder);

  readonly category = input.required<Category>();
  readonly close = output<void>();
  readonly saved = output<void>();
  readonly deleted = output<void>();

  protected readonly isDeleting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly isSaving = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required]],
    description: [''],
  });

  ngOnInit(): void {
    const current = this.category();
    this.form.setValue({
      name: current.name,
      description: current.description ?? '',
    });
  }

  save(): void {
    if (this.form.invalid) {
      return;
    }
    this.error.set(null);
    this.isSaving.set(true);

    const { name, description } = this.form.getRawValue();

    this.categoryApi.update(this.category().id, { name, description: description || null }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saved.emit();
      },
      error: (err) => {
        this.isSaving.set(false);
        if (err.status === 403) {
          this.error.set('You do not have permission to edit categories.');
        } else if (err.status === 409) {
          this.error.set('A category with that name already exists.');
        } else {
          this.error.set('Could not save the changes.');
        }
      },
    });
  }

  remove(): void {
    if (!confirm(`Delete category "${this.category().name}"? This cannot be undone.`)) {
      return;
    }
    this.error.set(null);
    this.isDeleting.set(true);

    this.categoryApi.delete(this.category().id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.deleted.emit();
      },
      error: (err) => {
        this.isDeleting.set(false);
        if (err.status === 403) {
          this.error.set('You do not have permission to delete categories (requires Admin).');
        } else {
          this.error.set('Could not delete the category.');
        }
      },
    });
  }


}
