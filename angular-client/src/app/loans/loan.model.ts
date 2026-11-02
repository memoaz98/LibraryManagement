/** Stored status of a loan. Nothing stores "Overdue" yet: read `isOverdue` instead. */
export type LoanStatus = 'Active' | 'Returned' | 'Overdue' | 'Lost';

/** A loan as returned by the API. Dates are UTC calendar dates (`yyyy-MM-dd`). */
export interface Loan {
  id: number;
  memberId: number;
  memberName: string;
  bookCopyId: number;
  inventoryCode: string;
  bookId: number;
  bookTitle: string;
  status: LoanStatus;
  loanDate: string;
  dueDate: string;
  returnDate: string | null;
  isOverdue: boolean;
}
