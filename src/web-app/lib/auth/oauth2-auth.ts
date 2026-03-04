'use client';

import axios, { AxiosError } from 'axios';

const AUTH_SERVER_URL = process.env.NEXT_PUBLIC_AUTH_URL || 'http://localhost:5000';
const CLIENT_ID = 'web-app';
const STORAGE_KEY_PREFIX = 'oauth2_';
const GRANTED_SCOPES_STORAGE_KEY = 'grantedScopes';
const DEFAULT_AUTH_SCOPES = 'openid profile email offline_access roles';

export type AuthenticatedUser = {
  id?: string;
  sub?: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
  isActive?: boolean;
  createdAt?: string;
};

type TokenResponse = {
  accessToken: string;
  refreshToken?: string;
  idToken?: string;
  scope?: string;
};

function getRedirectUri(): string {
  if (typeof window === 'undefined') {
    throw new Error('OAuth2Auth can only be used on the client side');
  }

  return `${window.location.origin}/callback`;
}

function generateRandomString(length: number): string {
  const charset = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
  const randomValues = crypto.getRandomValues(new Uint8Array(length));

  return Array.from(randomValues)
    .map((byte) => charset[byte % charset.length])
    .join('');
}

function storeFlowData(key: string, value: string): void {
  sessionStorage.setItem(`${STORAGE_KEY_PREFIX}${key}`, value);
}

function getFlowData(key: string): string | null {
  return sessionStorage.getItem(`${STORAGE_KEY_PREFIX}${key}`);
}

function clearFlowData(): void {
  Object.keys(sessionStorage).forEach((key) => {
    if (key.startsWith(STORAGE_KEY_PREFIX)) {
      sessionStorage.removeItem(key);
    }
  });
}

function postLogoutRedirect(idToken: string): void {
  const form = document.createElement('form');

  form.method = 'POST';
  form.action = `${AUTH_SERVER_URL}/connect/logout`;
  form.style.display = 'none';

  const idTokenInput = document.createElement('input');
  idTokenInput.type = 'hidden';
  idTokenInput.name = 'id_token_hint';
  idTokenInput.value = idToken;
  form.appendChild(idTokenInput);

  const redirectInput = document.createElement('input');
  redirectInput.type = 'hidden';
  redirectInput.name = 'post_logout_redirect_uri';
  redirectInput.value = window.location.origin;
  form.appendChild(redirectInput);

  document.body.appendChild(form);
  form.submit();
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const digest = await crypto.subtle.digest('SHA-256', data);
  const base64 = btoa(String.fromCharCode(...new Uint8Array(digest)));

  return base64
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

async function exchangeCodeForToken(
  code: string,
  codeVerifier: string
): Promise<TokenResponse> {
  const formData = new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: CLIENT_ID,
    code,
    redirect_uri: getRedirectUri(),
    code_verifier: codeVerifier,
  });

  try {
    const response = await axios.post(`${AUTH_SERVER_URL}/connect/token`, formData, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });

    return {
      accessToken: response.data.access_token,
      refreshToken: response.data.refresh_token,
      idToken: response.data.id_token,
      scope: response.data.scope,
    };
  } catch (error) {
    if (error instanceof AxiosError) {
      const errorMessage = error.response?.data?.error_description ||
        error.response?.data?.error ||
        error.message;

      throw new Error(`Token exchange failed: ${errorMessage}`);
    }

    throw error;
  }
}

async function getUserProfile(accessToken: string): Promise<AuthenticatedUser> {
  try {
    const response = await axios.get(`${AUTH_SERVER_URL}/connect/userinfo`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    return response.data;
  } catch (error) {
    if (error instanceof AxiosError) {
      const errorMessage = error.response?.data?.error_description ||
        error.response?.data?.error ||
        error.message;

      throw new Error(`Failed to get user info: ${errorMessage}`);
    }

    throw error;
  }
}

export class OAuth2Auth {
  static async authorize(): Promise<void> {
    const codeVerifier = generateRandomString(128);
    const codeChallenge = await generateCodeChallenge(codeVerifier);
    const state = generateRandomString(32);

    storeFlowData('code_verifier', codeVerifier);
    storeFlowData('state', state);

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: CLIENT_ID,
      redirect_uri: getRedirectUri(),
      scope: DEFAULT_AUTH_SCOPES,
      state,
      code_challenge: codeChallenge,
      code_challenge_method: 'S256',
    });

    window.location.href = `${AUTH_SERVER_URL}/connect/authorize?${params.toString()}`;
  }

  static async handleCallback(): Promise<{
    accessToken: string;
    refreshToken?: string;
    user: AuthenticatedUser;
  }> {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    const error = urlParams.get('error');

    if (error) {
      const errorDescription = urlParams.get('error_description') || 'Authorization failed';
      throw new Error(`OAuth2 error: ${error} - ${errorDescription}`);
    }

    if (!code || !state) {
      throw new Error('Missing authorization code or state parameter');
    }

    const savedState = getFlowData('state');
    if (!savedState || savedState !== state) {
      clearFlowData();
      throw new Error('Invalid state parameter');
    }

    const codeVerifier = getFlowData('code_verifier');
    if (!codeVerifier) {
      clearFlowData();
      throw new Error('Missing code verifier');
    }

    try {
      const tokenResponse = await exchangeCodeForToken(code, codeVerifier);

      localStorage.setItem('accessToken', tokenResponse.accessToken);
      if (tokenResponse.refreshToken) {
        localStorage.setItem('refreshToken', tokenResponse.refreshToken);
      }
      if (tokenResponse.idToken) {
        localStorage.setItem('idToken', tokenResponse.idToken);
      }
      this.setGrantedScopes(tokenResponse.scope);

      const userProfile = await getUserProfile(tokenResponse.accessToken);
      localStorage.setItem('user', JSON.stringify(userProfile));

      clearFlowData();
      window.history.replaceState({}, document.title, window.location.pathname);

      return {
        accessToken: tokenResponse.accessToken,
        refreshToken: tokenResponse.refreshToken,
        user: userProfile,
      };
    } catch (error) {
      clearFlowData();
      throw error;
    }
  }

  static async refreshToken(): Promise<TokenResponse> {
    const refreshToken = localStorage.getItem('refreshToken');

    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const formData = new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: CLIENT_ID,
      refresh_token: refreshToken,
    });

    try {
      const response = await axios.post(`${AUTH_SERVER_URL}/connect/token`, formData, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
      });

      localStorage.setItem('accessToken', response.data.access_token);
      if (response.data.refresh_token) {
        localStorage.setItem('refreshToken', response.data.refresh_token);
      }
      if (response.data.id_token) {
        localStorage.setItem('idToken', response.data.id_token);
      }
      if (typeof response.data.scope === 'string') {
        localStorage.setItem(GRANTED_SCOPES_STORAGE_KEY, response.data.scope);
      }

      return {
        accessToken: response.data.access_token,
        refreshToken: response.data.refresh_token,
        idToken: response.data.id_token,
        scope: response.data.scope,
      };
    } catch (error) {
      if (error instanceof AxiosError) {
        const errorMessage = error.response?.data?.error_description ||
          error.response?.data?.error ||
          error.message;

        throw new Error(`Token refresh failed: ${errorMessage}`);
      }

      throw error;
    }
  }

  static async getUserInfo(): Promise<AuthenticatedUser> {
    const accessToken = localStorage.getItem('accessToken');

    if (!accessToken) {
      throw new Error('No access token available');
    }

    return getUserProfile(accessToken);
  }

  static async logout(): Promise<void> {
    const idToken = localStorage.getItem('idToken');

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('idToken');
    localStorage.removeItem('user');
    localStorage.removeItem(GRANTED_SCOPES_STORAGE_KEY);
    clearFlowData();

    if (idToken) {
      postLogoutRedirect(idToken);
      return;
    }

    window.location.href = '/login';
  }

  static getAccessToken(): string | null {
    return typeof window === 'undefined' ? null : localStorage.getItem('accessToken');
  }

  static getUser(): AuthenticatedUser | null {
    if (typeof window === 'undefined') {
      return null;
    }

    const userValue = localStorage.getItem('user');
    if (!userValue) {
      return null;
    }

    try {
      return JSON.parse(userValue) as AuthenticatedUser;
    } catch {
      return null;
    }
  }

  static getScopes(): string[] {
    if (typeof window === 'undefined') {
      return [];
    }

    const scopeValue = localStorage.getItem(GRANTED_SCOPES_STORAGE_KEY);
    if (!scopeValue) {
      return [];
    }

    return scopeValue
      .split(' ')
      .map((scope) => scope.trim())
      .filter(Boolean);
  }

  static setGrantedScopes(scopeValue?: string | null): void {
    if (!scopeValue) {
      localStorage.removeItem(GRANTED_SCOPES_STORAGE_KEY);
      return;
    }

    localStorage.setItem(GRANTED_SCOPES_STORAGE_KEY, scopeValue);
  }
}

export default OAuth2Auth;
