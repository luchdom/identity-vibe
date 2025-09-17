import { api } from './api';

/**
 * OAuth2 Authorization Code Flow with PKCE implementation
 */
export class OAuth2Auth {
  private static readonly CLIENT_ID = 'web-admin';
  private static readonly REDIRECT_URI = `${window.location.origin}/callback`;
  private static readonly AUTH_SERVER_URL = process.env.NEXT_PUBLIC_GATEWAY_URL || 'http://localhost:5002';
  private static readonly STORAGE_KEY_PREFIX = 'oauth2_';

  /**
   * Generate a cryptographically random string
   */
  private static generateRandomString(length: number): string {
    const charset = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~';
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
      redirect_uri: this.REDIRECT_URI,
      scope: 'openid profile email offline_access orders.read orders.write profile.read profile.write',
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
  ): Promise<{ accessToken: string; refreshToken?: string; idToken?: string }> {
    const formData = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: this.CLIENT_ID,
      code: code,
      redirect_uri: this.REDIRECT_URI,
      code_verifier: codeVerifier,
    });

    const response = await fetch(`${this.AUTH_SERVER_URL}/connect/token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: formData.toString(),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Token exchange failed: ${error}`);
    }

    const data = await response.json();

    return {
      accessToken: data.access_token,
      refreshToken: data.refresh_token,
      idToken: data.id_token,
    };
  }

  /**
   * Get user profile using access token
   */
  private static async getUserProfile(accessToken: string): Promise<any> {
    // First try userinfo endpoint
    try {
      const response = await fetch(`${this.AUTH_SERVER_URL}/connect/userinfo`, {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
        },
      });

      if (response.ok) {
        return await response.json();
      }
    } catch (error) {
      console.warn('Userinfo endpoint failed, falling back to profile API', error);
    }

    // Fallback to profile API
    const previousToken = localStorage.getItem('accessToken');
    localStorage.setItem('accessToken', accessToken); // Temporarily set for API interceptor

    try {
      const response = await api.get('/account/profile');
      return response.data;
    } finally {
      if (previousToken) {
        localStorage.setItem('accessToken', previousToken);
      }
    }
  }

  /**
   * Refresh access token using refresh token
   */
  static async refreshToken(): Promise<{ accessToken: string; refreshToken?: string }> {
    const refreshToken = localStorage.getItem('refreshToken');

    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const formData = new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: this.CLIENT_ID,
      refresh_token: refreshToken,
    });

    const response = await fetch(`${this.AUTH_SERVER_URL}/connect/token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: formData.toString(),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Token refresh failed: ${error}`);
    }

    const data = await response.json();

    // Update stored tokens
    localStorage.setItem('accessToken', data.access_token);
    if (data.refresh_token) {
      localStorage.setItem('refreshToken', data.refresh_token);
    }

    return {
      accessToken: data.access_token,
      refreshToken: data.refresh_token,
    };
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
}

export default OAuth2Auth;