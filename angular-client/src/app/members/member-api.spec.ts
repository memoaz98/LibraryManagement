import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { MemberApi } from './member-api';

describe('MemberApi', () => {
  const baseUrl = 'https://localhost:7281/api/members';
  let service: MemberApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MemberApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should send a trimmed search term', () => {
    service.search('  mora ').subscribe();

    const req = http.expectOne((r) => r.url === baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('mora');
  });

  it('should omit a blank search term', () => {
    service.search('   ').subscribe();

    expect(http.expectOne(baseUrl).request.params.has('search')).toBe(false);
  });
});
