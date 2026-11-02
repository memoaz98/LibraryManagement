/** `active`: open loans (Active or Overdue). `returned`: closed loans. */
export type LoanStatusFilter = 'active' | 'returned';

/** Filters for the staff loan list. A missing field does not filter. */
export interface LoanQuery {
  memberId?: number;
  bookCopyId?: number;
  status?: LoanStatusFilter;
}
