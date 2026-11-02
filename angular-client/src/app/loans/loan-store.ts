import { Service, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { LoanApi } from './loan-api';
import { Loan } from './loan.model';
import { CreateLoan } from './create-loan.model';
import { LoanQuery } from './loan-query.model';
import { loanErrorMessage } from './loan-error-message';

/**
 * State of the staff loans page: the loan list, the filters it was loaded
 * with, progress flags, and the last error or success message.
 *
 * Writes update the list from the API's response instead of reloading it.
 */
@Service()
export class LoanStore {
  private readonly loanApi = inject(LoanApi);

  private readonly loansState = signal<Loan[]>([]);
  private readonly queryState = signal<LoanQuery>({});
  private readonly loadingState = signal(false);
  private readonly savingState = signal(false);
  private readonly returningIdState = signal<number | null>(null);
  private readonly errorState = signal<string | null>(null);
  private readonly noticeState = signal<string | null>(null);

  // Ignores a slow response that a newer load already replaced.
  private loadSequence = 0;

  readonly loans = this.loansState.asReadonly();
  readonly query = this.queryState.asReadonly();
  readonly isLoading = this.loadingState.asReadonly();
  readonly isSaving = this.savingState.asReadonly();
  readonly returningId = this.returningIdState.asReadonly();
  readonly error = this.errorState.asReadonly();
  readonly notice = this.noticeState.asReadonly();

  readonly overdueCount = computed(() => this.loansState().filter((loan) => loan.isOverdue).length);

  /** Loads the loans for `query`, or reloads them with the current filters. */
  async load(query: LoanQuery = this.queryState()): Promise<void> {
    const sequence = ++this.loadSequence;
    this.queryState.set(query);
    this.loadingState.set(true);
    this.errorState.set(null);

    try {
      const loans = await firstValueFrom(this.loanApi.getAll(query));
      if (sequence === this.loadSequence) {
        this.loansState.set(loans);
      }
    } catch (error) {
      if (sequence === this.loadSequence) {
        this.loansState.set([]);
        this.errorState.set(loanErrorMessage(error, 'load the loans'));
      }
    } finally {
      if (sequence === this.loadSequence) {
        this.loadingState.set(false);
      }
    }
  }

  /** Lends a copy. Resolves to whether it worked; the reason is in `error` if not. */
  async checkout(request: CreateLoan): Promise<boolean> {
    this.clearMessages();
    this.savingState.set(true);

    try {
      const loan = await firstValueFrom(this.loanApi.checkout(request));
      if (matchesQuery(loan, this.queryState())) {
        this.loansState.update((loans) => [loan, ...loans]);
      }
      this.noticeState.set(
        `Lent "${loan.bookTitle}" (copy ${loan.inventoryCode}) to ${loan.memberName}, due ${loan.dueDate}.`,
      );
      return true;
    } catch (error) {
      this.errorState.set(loanErrorMessage(error, 'create the loan'));
      return false;
    } finally {
      this.savingState.set(false);
    }
  }

  /** Registers a return. Resolves to whether it worked; the reason is in `error` if not. */
  async returnLoan(loanId: number): Promise<boolean> {
    this.clearMessages();
    this.returningIdState.set(loanId);

    try {
      const loan = await firstValueFrom(this.loanApi.returnLoan(loanId));
      // A returned loan leaves an "open loans" list; otherwise it is updated in place.
      this.loansState.update((loans) =>
        matchesQuery(loan, this.queryState())
          ? loans.map((current) => (current.id === loan.id ? loan : current))
          : loans.filter((current) => current.id !== loan.id),
      );
      this.noticeState.set(`"${loan.bookTitle}" (copy ${loan.inventoryCode}) was returned.`);
      return true;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        // The row was out of date (e.g. already returned elsewhere): refresh the list.
        void this.load();
      }
      this.errorState.set(loanErrorMessage(error, 'return the loan'));
      return false;
    } finally {
      this.returningIdState.set(null);
    }
  }

  clearMessages(): void {
    this.errorState.set(null);
    this.noticeState.set(null);
  }
}

/** Whether `loan` belongs in a list loaded with `query`. */
function matchesQuery(loan: Loan, query: LoanQuery): boolean {
  if (query.memberId !== undefined && loan.memberId !== query.memberId) {
    return false;
  }
  if (query.bookCopyId !== undefined && loan.bookCopyId !== query.bookCopyId) {
    return false;
  }
  if (query.status === 'active') {
    return loan.status === 'Active' || loan.status === 'Overdue';
  }
  if (query.status === 'returned') {
    return loan.status === 'Returned';
  }
  return true;
}
