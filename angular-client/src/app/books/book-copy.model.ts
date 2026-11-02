export type CopyStatus = 'Available' | 'Borrowed' | 'Maintenance' | 'Lost';

export interface BookCopy {
  id: number;
  bookId: number;
  inventoryCode: string;
  status: CopyStatus;
  /** UTC calendar date (`yyyy-MM-dd`). */
  acquisitionDate: string;
}
