import { HttpErrorResponse } from '@angular/common/http';
import { LoanConflictCode, LoanConflictProblem } from './loan-conflict.model';

const CONFLICT_MESSAGES: Record<LoanConflictCode, string> = {
  member_inactive: 'This member is inactive and cannot borrow books.',
  member_has_overdue_loan:
    'This member has an overdue loan. It must be returned before they can borrow again.',
  loan_limit_reached: 'This member already has the maximum number of open loans.',
  book_deleted: 'This book was removed from the catalog, so its copies cannot be lent.',
  copy_not_available:
    'This copy is not available. Someone may have just borrowed it: pick another copy.',
  same_book_on_loan: 'This member already has a copy of this book on loan.',
  loan_not_open: 'This loan is already closed, so it cannot be returned again.',
};

/**
 * Explains a failed loan-feature request to the user. `action` completes the
 * sentence "Could not ...", e.g. "create the loan".
 */
export function loanErrorMessage(error: unknown, action: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return `Could not ${action}. Please try again.`;
  }

  const problem = error.error as Partial<LoanConflictProblem> | null;

  switch (error.status) {
    case 0:
      return `Could not ${action}: the server cannot be reached. Check your connection and try again.`;
    case 400:
      return `Could not ${action}: the request was not valid.`;
    case 401:
      return 'Your session has expired. Please log in again.';
    case 403:
      return `Could not ${action}: you need the Librarian or Administrator role.`;
    case 404:
      return `Could not ${action}: ${problem?.detail ?? 'it was not found.'}`;
    case 409:
      return (
        (problem?.code && CONFLICT_MESSAGES[problem.code]) ??
        `Could not ${action}: ${problem?.detail ?? 'it conflicts with the current data.'}`
      );
    default:
      return `Could not ${action}: the server had an unexpected problem. Please try again later.`;
  }
}
