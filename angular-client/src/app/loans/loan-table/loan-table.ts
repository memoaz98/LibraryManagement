import { Component, input, output } from '@angular/core';
import { Loan } from '../loan.model';

/** Presentational loans table. It reports which loan to return; the parent returns it. */
@Component({
  selector: 'app-loan-table',
  templateUrl: './loan-table.html',
  styleUrl: './loan-table.scss',
})
export class LoanTable {
  readonly loans = input.required<Loan[]>();
  /** The loan being returned right now, if any. */
  readonly returningId = input<number | null>(null);

  readonly returnLoan = output<Loan>();

  protected isOpen(loan: Loan): boolean {
    return loan.status === 'Active' || loan.status === 'Overdue';
  }
}
