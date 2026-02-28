import axios, { AxiosError } from 'axios';
import { authApi } from './api';

/**
 * OAuth2 Authorization Code Flow with PKCE implementation
 */
export class OAuth2Auth {
  private static readonly CLIENT_ID = 'web-admin';
  private static readonly AUTH_SERVER_URL = process.env.NEXT_PUBLIC_AUTH_URL || 'http://localhost:5000';
  private static readonly STORAGE_KEY_PREFIX = 'oauth2_';
  private static readonly GRANTED_SCOPES_STORAGE_KEY = 'grantedScopes';
  private static readonly DEFAULT_AUTH_SCOPES = 'openid profile email offline_access roles';

  /**
   * Get redirect URI dynamically (client-side only)
   */
  private static getRedirectUri(): string {
    if (typeof window === 'undefined') {
      throw new Error('OAuth2Auth can only be used on the client side');
    }
    return `${window.location.origin}/callback`;
  }

  /**
   * Generate a cryptographically random string
   */
  private static generateRandomString(length: number): string {
    const charset = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
    const randomValues = crypto.getRandomValues(new Uint8Array(length));
    return Array.from(randomValues)
      .map(byte => charset[byte % charset.length])
      .join('');
  }

  /**
   * Generate PKCE code verifier
   */
  private static generateCodeVerifier(): string {
    return this.generateRandomString(128);
  }

  /**
   * Generate PKCE code challenge from verifier
   */
  private static async generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);

    // Convert to base64url
    const base64 = btoa(String.fromCharCode(...new Uint8Array(digest)));
    return base64
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }

  /**
   * Generate state parameter for CSRF protection
   */
  private static generateState(): string {
    return this.generateRandomString(32);
  }

  /**
   * Store OAuth2 flow data in session storage
   */
  private static storeFlowData(key: string, value: string): void {
    sessionStorage.setItem(`${this.STORAGE_KEY_PREFIX}${key}`, value);
  }

  /**
   * Retrieve OAuth2 flow data from session storage
   */
  private static getFlowData(key: string): string | null {
    return sessionStorage.getItem(`${this.STORAGE_KEY_PREFIX}${key}`);
  }

  /**
   * Clear OAuth2 flow data from session storage
   */
  private static clearFlowData(): void {
    const keys = Object.keys(sessionStorage);
    keys.forEach(key => {
      if (key.startsWith(this.STORAGE_KEY_PREFIX)) {
        sessionStorage.removeItem(key);
      }
    });
  }

  /**
   * Initiate the OAuth2 authorization code flow with PKCE
   */
  static async authorize(): Promise<void> {
    // Generate PKCE parameters
    const codeVerifier = this.generateCodeVerifier();
    const codeChallenge = await this.generateCodeChallenge(codeVerifier);
    const state = this.generateState();

    // Store for later use
    this.storeFlowData('code_verifier', codeVerifier);
    this.storeFlowData('state', state);

    // Build authorization URL
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: this.CLIENT_ID,
      redirect_uri: this.getRedirectUri(),
      scope: this.DEFAULT_AUTH_SCOPES,
      state: state,
      code_challenge: codeChallenge,
      code_challenge_method: 'S256',
    });

    const authorizationUrl = `${this.AUTH_SERVER_URL}/connect/authorize?${params.toString()}`;

    // Redirect to authorization server
    window.location.href = authorizationUrl;
  }

  /**
   * Handle the authorization callback
   */
  static async handleCallback(): Promise<{ accessToken: string; refreshToken?: string; user: any }> {
    // Parse URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    const error = urlParams.get('error');

    // Check for errors
    if (error) {
      const errorDescription = urlParams.get('error_description') || 'Authorization failed';
      throw new Error(`OAuth2 Error: ${error} - ${errorDescription}`);
    }

    // Validate required parameters
    if (!code || !state) {
      throw new Error('Missing authorization code or state parameter');
    }

    // Validate state to prevent CSRF
    const savedState = this.getFlowData('state');
    if (!savedState || savedState !== state) {
      this.clearFlowData();
      throw new Error('Invalid state parameter - possible CSRF attack');
    }

    // Get code verifier
    const codeVerifier = this.getFlowData('code_verifier');
    if (!codeVerifier) {
      throw new Error('Missing code verifier');
    }

    try {
      // Exchange authorization code for tokens
      const tokenResponse = await this.exchangeCodeForToken(code, codeVerifier);

      // Store tokens
      localStorage.setItem('accessToken', tokenResponse.accessToken);
      if (tokenResponse.refreshToken) {
        localStorage.setItem('refreshToken', tokenResponse.refreshToken);
      }
      this.setGrantedScopes(tokenResponse.scope);

      // Get user profile
      const userProfile = await this.getUserProfile(tokenResponse.accessToken);

      // Store user profile
      localStorage.setItem('user', JSON.stringify(userProfile));

      // Clear flow data
      this.clearFlowData();

      // Clear URL parameters
      window.history.replaceState({}, document.title, window.location.pathname);

      return {
        accessToken: tokenResponse.accessToken,
        refreshToken: tokenResponse.refreshToken,
        user: userProfile,
      };
    } catch (error) {
      this.clearFlowData();
      throw error;
    }
  }

  /**
   * Exchange authorization code for access token
   */
  private static async exchangeCodeForToken(
    code: string,
    codeVerifier: string
  ): Promise<{ accessToken: string; refreshToken?: string; idToken?: string; scope?: string }> {
    const formData = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: this.CLIENT_ID,
      code: code,
      redirect_uri: this.getRedirectUri(),
      code_verifier: codeVerifier,
    });

    try {
      const response = await axios.post(`${this.AUTH_SERVER_URL}/connect/token`, formData, {
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
                            error.response?.data ||
                            error.message;
        throw new Error(`Token exchange failed: ${errorMessage}`);
      }
      throw new Error(`Token exchange failed: ${error}`);
    }
  }

  /**
   * Get user profile using access token
   */
  private static async getUserProfile(accessToken: string): Promise<any> {
    try {
      const response = await axios.get(`${this.AUTH_SERVER_URL}/connect/userinfo`, {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
        },
      });
      return response.data;
    } catch (error) {
      if (error instanceof AxiosError) {
        const errorMessage = error.response?.data?.message ||
                            error.response?.data?.error_description ||
                            error.response?.data?.error ||
                            error.message;
        throw new Error(`Failed to get user info: ${errorMessage}`);
      }
      throw error;
    }
  }

  /**
   * Refresh access token using refresh token
   */
  static async refreshToken(): Promise<{ accessToken: string; refreshToken?: string; scope?: string }> {
    const refreshToken = localStorage.getItem('refreshToken');

    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const formData = new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: this.CLIENT_ID,
      refresh_token: refreshToken,
    });

    try {
      const response = await axios.post(`${this.AUTH_SERVER_URL}/connect/token`, formData, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
      });

      // Update stored tokens
      localStorage.setItem('accessToken', response.data.access_token);
      if (response.data.refresh_token) {
        localStorage.setItem('refreshToken', response.data.refresh_token);
      }
      if (typeof response.data.scope === 'string') {
        localStorage.setItem(this.GRANTED_SCOPES_STORAGE_KEY, response.data.scope);
      }

      return {
        accessToken: response.data.access_token,
        refreshToken: response.data.refresh_token,
        scope: response.data.scope,
      };
    } catch (error) {
      if (error instanceof AxiosError) {
        const errorMessage = error.response?.data?.error_description ||
                            error.response?.data?.error ||
                            error.response?.data ||
                            error.message;
        throw new Error(`Token refresh failed: ${errorMessage}`);
      }
      throw new Error(`Token refresh failed: ${error}`);
    }
  }

  /**
   * Logout user and revoke tokens
   */
  static async logout(): Promise<void> {
    const idToken = localStorage.getItem('idToken');

    // Clear local storage
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('idToken');
    localStorage.removeItem('user');
    localStorage.removeItem(this.GRANTED_SCOPES_STORAGE_KEY);

    // Clear session storage
    this.clearFlowData();

    // Redirect to logout endpoint if we have an ID token
    if (idToken) {
      const params = new URLSearchParams({
        id_token_hint: idToken,
        post_logout_redirect_uri: window.location.origin,
      });

      window.location.href = `${this.AUTH_SERVER_URL}/connect/logout?${params.toString()}`;
    } else {
      // Just redirect to home
      window.location.href = '/';
    }
  }

  /**
   * Check if user is authenticated
   */
  static isAuthenticated(): boolean {
    return !!localStorage.getItem('accessToken');
  }

  /**
   * Get stored access token
   */
  static getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  /**
   * Get stored user profile
   */
  static getUser(): any | null {
    const userStr = localStorage.getItem('user');
    if (!userStr) return null;

    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }

  static getScopes(): string[] {
    const scopeValue = localStorage.getItem(this.GRANTED_SCOPES_STORAGE_KEY);
    if (!scopeValue) {
      return [];
    }

    return scopeValue
      .split(' ')
      .map(scope => scope.trim())
      .filter(Boolean);
  }

  /**
   * Get user profile from server (moved from auth-api.ts)
   */
  static async getUserInfo(): Promise<any> {
    try {
      const response = await authApi.get('/connect/userinfo');
      return response.data;
    } catch (error) {
      if (error instanceof AxiosError) {
        const errorMessage = error.response?.data?.message ||
                            error.response?.data?.error ||
                            error.message;
        throw new Error(`Failed to get user profile: ${errorMessage}`);
      }
      throw error;
    }
  }

  static setGrantedScopes(scopeValue?: string | null): void {
    if (!scopeValue) {
      localStorage.removeItem(this.GRANTED_SCOPES_STORAGE_KEY);
      return;
    }

    localStorage.setItem(this.GRANTED_SCOPES_STORAGE_KEY, scopeValue);
  }

}

export default OAuth2Auth;
