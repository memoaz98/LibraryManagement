import { defineConfig } from 'cypress';

export default defineConfig({
  video: false,
  viewportWidth: 1280,
  viewportHeight: 800,

  // Non-sensitive configuration. Read synchronously: Cypress.expose('apiUrl').
  expose: {
    apiUrl: 'https://localhost:7281/api',
  },

  // Sensitive values. Read through the chainable cy.env([...]), which keeps
  // them out of the command log and screenshots.
  env: {
    testUserEmail: 'guille@test.com',
    testUserPassword: 'Test1234!',
  },

  e2e: {
    baseUrl: 'http://localhost:4200',
    specPattern: 'cypress/e2e/**/*.cy.ts',
    supportFile: 'cypress/support/e2e.ts',

    retries: { runMode: 2, openMode: 0 },

    defaultCommandTimeout: 6000,
    requestTimeout: 10000,
    responseTimeout: 15000,

    setupNodeEvents(on, config) {
      return config;
    },
  },
});
