import { Component, input, output } from '@angular/core';
import { Member } from '../../members/member.model';
import { LoanQuery, LoanStatusFilter } from '../loan-query.model';

/** Presentational filters for the loans list. It reports the new query; the parent loads it. */
@Component({
  selector: 'app-loan-filters',
  templateUrl: './loan-filters.html',
  styleUrl: './loan-filters.scss',
})
export class LoanFilters {
  readonly members = input.required<Member[]>();
  readonly query = input.required<LoanQuery>();

  readonly queryChange = output<LoanQuery>();

  changeStatus(value: string): void {
    this.queryChange.emit({
      ...this.query(),
      status: value === '' ? undefined : (value as LoanStatusFilter),
    });
  }

  changeMember(value: string): void {
    this.queryChange.emit({
      ...this.query(),
      memberId: value === '' ? undefined : Number(value),
    });
  }
}
