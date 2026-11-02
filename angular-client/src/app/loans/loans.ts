import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { LoanStore } from './loan-store';
import { Loan } from './loan.model';
import { CreateLoan } from './create-loan.model';
import { LoanQuery } from './loan-query.model';
import { loanErrorMessage } from './loan-error-message';
import { CheckoutForm } from './checkout-form/checkout-form';
import { LoanFilters } from './loan-filters/loan-filters';
import { LoanTable } from './loan-table/loan-table';
import { MemberApi } from '../members/member-api';
import { Member } from '../members/member.model';
import { BookApi } from '../books/book-api';
import { Book } from '../books/book.model';
import { BookCopyApi } from '../books/book-copy-api';
import { BookCopy } from '../books/book-copy.model';

/**
 * Staff loans page (smart): it owns the data and the API calls, and feeds the
 * presentational checkout form, filters and table.
 */
@Component({
  selector: 'app-loans',
  imports: [CheckoutForm, LoanFilters, LoanTable],
  templateUrl: './loans.html',
  styleUrl: './loans.scss',
})
export class Loans {
  protected readonly store = inject(LoanStore);
  private readonly memberApi = inject(MemberApi);
  private readonly bookApi = inject(BookApi);
  private readonly bookCopyApi = inject(BookCopyApi);

  private readonly checkoutForm = viewChild.required(CheckoutForm);

  /** Every member, for the list filter. */
  protected readonly members = signal<Member[]>([]);
  /** Members matching the checkout search; null until the first results arrive. */
  protected readonly memberResults = signal<Member[] | null>(null);
  protected readonly books = signal<Book[]>([]);
  /** Available copies of the book picked in the form; null with no book or while loading. */
  protected readonly copies = signal<BookCopy[] | null>(null);

  // Errors of the lookups the page loads itself; the store reports its own.
  private readonly lookupError = signal<string | null>(null);
  protected readonly error = computed(() => this.lookupError() ?? this.store.error());

  private selectedBookId: number | null = null;

  constructor() {
    this.store.clearMessages();
    void this.store.load();
    this.loadMembers();
    this.loadBooks();
  }

  searchMembers(term: string): void {
    this.lookupError.set(null);
    this.memberApi.search(term).subscribe({
      next: (members) => this.memberResults.set(members),
      error: (err) => this.lookupError.set(loanErrorMessage(err, 'search the members')),
    });
  }

  loadCopies(bookId: number | null): void {
    this.selectedBookId = bookId;
    this.copies.set(null);
    if (bookId === null) {
      return;
    }

    this.bookCopyApi.getByBook(bookId, true).subscribe({
      next: (copies) => {
        // Ignore the answer if another book was picked meanwhile.
        if (this.selectedBookId === bookId) {
          this.copies.set(copies);
        }
      },
      error: (err) => this.lookupError.set(loanErrorMessage(err, 'load the copies')),
    });
  }

  async checkout(request: CreateLoan): Promise<void> {
    this.lookupError.set(null);

    if (await this.store.checkout(request)) {
      this.checkoutForm().reset();
    } else {
      // The copy list may be out of date, e.g. someone else just borrowed the copy.
      this.loadCopies(this.selectedBookId);
    }
  }

  async returnLoan(loan: Loan): Promise<void> {
    this.lookupError.set(null);
    await this.store.returnLoan(loan.id);
  }

  filter(query: LoanQuery): void {
    void this.store.load(query);
  }

  private loadMembers(): void {
    this.memberApi.search().subscribe({
      next: (members) => {
        this.members.set(members);
        this.memberResults.set(members);
      },
      error: (err) => this.lookupError.set(loanErrorMessage(err, 'load the members')),
    });
  }

  private loadBooks(): void {
    this.bookApi.getAll().subscribe({
      next: (books) => this.books.set(books),
      error: (err) => this.lookupError.set(loanErrorMessage(err, 'load the books')),
    });
  }
}
