import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { CategoryDetail } from './category-detail';
import { Category } from '../category.model';

describe('CategoryDetail', () => {
  let component: CategoryDetail;
  let fixture: ComponentFixture<CategoryDetail>;

  const category: Category = { id: 7, name: 'Fiction', description: 'Novels and short stories' };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryDetail],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryDetail);
    component = fixture.componentInstance;

    // `category` is an input.required(): it must be bound before the first
    // change detection run, otherwise ngOnInit throws NG0950.
    fixture.componentRef.setInput('category', category);

    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should prefill the form with the selected category', () => {
    const element = fixture.nativeElement as HTMLElement;
    const name = element.querySelector('input[formControlName="name"]') as HTMLInputElement;
    const description = element.querySelector('input[formControlName="description"]') as HTMLInputElement;

    expect(name.value).toBe('Fiction');
    expect(description.value).toBe('Novels and short stories');
  });

  it('should emit close when Cancel is clicked', () => {
    let closed = false;
    component.close.subscribe(() => (closed = true));

    const cancel = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.trim() === 'Cancel');

    cancel?.click();

    expect(closed).toBe(true);
  });
});
