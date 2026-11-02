import { HttpErrorResponse } from '@angular/common/http';

import { loanErrorMessage } from './loan-error-message';

const httpError = (status: number, error: unknown = null) =>
  new HttpErrorResponse({ status, error });

describe('loanErrorMessage', () => {
  it('should explain a 409 by its reason code', () => {
    const error = httpError(409, { status: 409, code: 'member_has_overdue_loan' });

    expect(loanErrorMessage(error, 'create the loan')).toBe(
      'This member has an overdue loan. It must be returned before they can borrow again.',
    );
  });

  it('should fall back to the server detail for a 409 without a known code', () => {
    const error = httpError(409, { status: 409, detail: 'Something clashed.' });

    expect(loanErrorMessage(error, 'create the loan')).toBe(
      'Could not create the loan: Something clashed.',
    );
  });

  it('should pass on the server detail of a 404', () => {
    const error = httpError(404, { status: 404, detail: "Member with id '5' was not found." });

    expect(loanErrorMessage(error, 'create the loan')).toBe(
      "Could not create the loan: Member with id '5' was not found.",
    );
  });

  it('should explain an expired session', () => {
    expect(loanErrorMessage(httpError(401), 'load the loans')).toBe(
      'Your session has expired. Please log in again.',
    );
  });

  it('should explain a missing role', () => {
    expect(loanErrorMessage(httpError(403), 'return the loan')).toContain(
      'Librarian or Administrator',
    );
  });

  it('should explain an unreachable server', () => {
    expect(loanErrorMessage(httpError(0), 'load the loans')).toContain('cannot be reached');
  });

  it('should explain an unexpected server error', () => {
    expect(loanErrorMessage(httpError(500), 'load the loans')).toContain('unexpected problem');
  });
});
