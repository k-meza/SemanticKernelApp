import { Amplify } from 'aws-amplify';

const scopes = (import.meta.env.VITE_COGNITO_SCOPES || '')
  .split(',')
  .map((s: string) => s.trim())
  .filter(Boolean);

Amplify.configure({
  Auth: {
    Cognito: {
      // core Cognito config (v6 style)
      userPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID,
      userPoolClientId: import.meta.env.VITE_COGNITO_APP_CLIENT_ID,

      // sign-in options you want to allow with your *own* UI:
      loginWith: {
        email: true,
        // username: true, // toggle if you want username auth instead
        // Hosted UI (PKCE) – recommended for SPAs:
        oauth: {
          domain: import.meta.env.VITE_COGNITO_DOMAIN,
          scopes,
          redirectSignIn: [import.meta.env.VITE_COGNITO_REDIRECT_SIGNIN],
          redirectSignOut: [import.meta.env.VITE_COGNITO_REDIRECT_SIGNOUT],
          responseType: 'code', // Authorization Code + PKCE
        },
      },
    },
  },
});
