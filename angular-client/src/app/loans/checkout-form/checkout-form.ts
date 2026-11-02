import { Component, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Member } from '../../members/member.model';
import { Book } from '../../books/book.model';
import { BookCopy } from '../../books/book-copy.model';
import { CreateLoan } from '../create-loan.model';

/**
 * Presentational checkout form. It shows the members, books and copies it is
 * given and reports what the librarian picks; the parent loads the data and
 * calls the API.
 */
@Component({
  selector: 'app-checkout-form',
  imports: [ReactiveFormsModule],
  templateUrl: './checkout-form.html',
  styleUrl: './checkout-form.scss',
})
export class CheckoutForm {
  private readonly formBuilder = inject(FormBuilder);

  /** Members matching the last search; null until the first results arrive. */
  readonly members = input.required<Member[] | null>();
  readonly books = input.required<Book[]>();
  /** Available copies of the picked book; null while there is no book or they are loading. */
  readonly copies = input.required<BookCopy[] | null>();
  readonly isSaving = input(false);

  readonly searchMembers = output<string>();
  readonly bookSelected = output<number | null>();
  readonly checkout = output<CreateLoan>();

  protected readonly form = this.formBuilder.nonNullable.group({
    search: [''],
    memberId: [null as number | null, [Validators.required]],
    bookId: [null as number | null, [Validators.required]],
    bookCopyId: [null as number | null, [Validators.required]],
  });

  constructor() {
    // Another book means other copies: drop the copy picked and ask for the new ones.
    this.form.controls.bookId.valueChanges.pipe(takeUntilDestroyed()).subscribe((bookId) => {
      this.form.controls.bookCopyId.setValue(null);
      this.bookSelected.emit(bookId);
    });
  }

  search(): void {
    // The current pick may not be among the new results.
    this.form.controls.memberId.setValue(null);
    this.searchMembers.emit(this.form.controls.search.value);
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    const { memberId, bookCopyId } = this.form.getRawValue();
    this.checkout.emit({ memberId: memberId!, bookCopyId: bookCopyId! });
  }

  /** Clears the form. The parent calls it after a successful checkout. */
  reset(): void {
    this.form.reset();
  }
}
