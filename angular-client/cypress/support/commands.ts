/// <reference types="cypress" />

// Shape of the sensitive values declared in cypress.config.ts -> env.
interface TestCredentials {
  testUserEmail: string;
  testUserPassword: string;
}

// Keys owned by TokenStorage in the Angular app.
const ACCESS_TOKEN_KEY = 'library.accessToken';
const REFRESH_TOKEN_KEY = 'library.refreshToken';

/**
 * Queries by the data-cy contract instead of repeating the attribute selector.
 * cy.getByCy('login-email') === cy.get('[data-cy="login-email"]')
 */
Cypress.Commands.add('getByCy', (selector, options) => {
  return cy.get(`[data-cy="${selector}"]`, options);
});

/**
 * App Action: authenticates through the API instead of the login form, and
 * caches the resulting session so it is established once per run.
 *
 * The login FORM is covered end to end in auth/login.cy.ts. Every other spec
 * treats authentication as a precondition, not as the thing under test.
 */
Cypress.Commands.add('loginByApi', () => {
  const apiUrl = Cypress.expose('apiUrl') as string;

  cy.env<TestCredentials>(['testUserEmail', 'testUserPassword']).then(
    ({ testUserEmail, testUserPassword }) => {
      cy.session(
        // Cache key. Including the email means a different user gets its own
        // cached session instead of silently reusing this one.
        ['user', testUserEmail],
        () => {
          cy.request({
            method: 'POST',
            url: `${apiUrl}/auth/login`,
            body: { email: testUserEmail, password: testUserPassword },
          })
            .its('body')
            .then((body) => {
              // Land on the app origin first, so the tokens are written to the
              // localStorage that the application will actually read.
              cy.visit('/');
              cy.window().then((win) => {
                win.localStorage.setItem(ACCESS_TOKEN_KEY, body.accessToken);
                win.localStorage.setItem(REFRESH_TOKEN_KEY, body.refreshToken);
              });
            });
        },
        // Reuse the session across spec files, not just across tests.
        { cacheAcrossSpecs: true },
      );
    },
  );
});

declare global {
  namespace Cypress {
    interface Chainable {
      /** Queries an element by its data-cy attribute. */
      getByCy(
        selector: string,
        options?: Partial<Loggable & Timeoutable & Withinable & Shadow>,
      ): Chainable<JQuery<HTMLElement>>;

      /** Authenticates the default test user through the API. */
      loginByApi(): Chainable<void>;
    }
  }
}

// Required so `declare global` is treated as a module augmentation.
export {};
