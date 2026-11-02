/** Why the API refused a checkout or a return (the `code` of its 409 responses). */
export type LoanConflictCode =
  | 'member_inactive'
  | 'member_has_overdue_loan'
  | 'loan_limit_reached'
  | 'book_deleted'
  | 'copy_not_available'
  | 'same_book_on_loan'
  | 'loan_not_open';

/** Body of a 409 from the loan endpoints: a ProblemDetails plus its reason code. */
export interface LoanConflictProblem {
  type: string;
  title: string;
  status: 409;
  detail: string;
  code: LoanConflictCode;
}
