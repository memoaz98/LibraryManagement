describe('Route protection', () => {
  // testIsolation clears localStorage between tests, so every test here
  // starts as a genuinely anonymous visitor. No manual cleanup needed.

  it('redirects an anonymous visitor away from /books', () => {
    cy.visit('/books');

    cy.location('pathname').should('eq', '/login');
    cy.get('[data-cy=login-form]').should('be.visible');
  });

  it('redirects an anonymous visitor away from /categories', () => {
    cy.visit('/categories');

    cy.location('pathname').should('eq', '/login');
  });

  it('leaves public routes reachable', () => {
    cy.visit('/');
    cy.location('pathname').should('eq', '/');

    cy.visit('/register');
    cy.location('pathname').should('eq', '/register');
  });

  it('lets an authenticated user reach the protected routes', () => {
    // Precondition, not the thing under test: taken by the fast path.
    cy.loginByApi();

    cy.visit('/books');
    cy.location('pathname').should('eq', '/books');
    cy.getByCy('nav-logout').should('be.visible');

    cy.visit('/categories');
    cy.location('pathname').should('eq', '/categories');
  });

});
