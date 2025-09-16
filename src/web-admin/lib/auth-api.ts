import { api } from './api';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken?: string;
}

export class AuthApi {
  private static readonly BASE_PATH = '/auth';

  /**
   * Authenticate user and get tokens using OAuth2 password flow
   */
  static async login(credentials: LoginRequest): Promise<LoginResponse> {
    // OAuth2 password flow request
    const formData = new URLSearchParams();
    formData.append('grant_type', 'password');
    formData.append('client_id', 'web-admin');
    formData.append('username', credentials.email);
    formData.append('password', credentials.password);
    formData.append('scope', 'openid profile email');

    const response = await api.post('/connect/token', formData, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });
    
    // Transform OAuth2 response to our interface
    const oauthData = response.data;
    
    // Store tokens BEFORE making profile request so api interceptor can use them
    localStorage.setItem('accessToken', oauthData.access_token);
    if (oauthData.refresh_token) {
      localStorage.setItem('refreshToken', oauthData.refresh_token);
    }
    
    const profileResponse = await api.get('/account/profile');
    
    return {
      accessToken: oauthData.access_token,
      refreshToken: oauthData.refresh_token,
      user: profileResponse.data,
    };
  }

  /**
   * Logout user and invalidate tokens
   */
  static async logout(): Promise<void> {
    await api.post('/connect/logout');
  }

  /**
   * Refresh access token using OAuth2 refresh token flow
   */
  static async refreshTokens(request: RefreshRequest): Promise<RefreshResponse> {
    const formData = new URLSearchParams();
    formData.append('grant_type', 'refresh_token');
    formData.append('client_id', 'web-admin');
    formData.append('refresh_token', request.refreshToken);

    const response = await api.post('/connect/token', formData, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
    });
    
    const oauthData = response.data;
    return {
      accessToken: oauthData.access_token,
      refreshToken: oauthData.refresh_token,
    };
  }

  /**
   * Get current user profile
   */
  static async getProfile(): Promise<User> {
    const response = await api.get<User>('/account/profile');
    return response.data;
  }

  /**
   * Store authentication data in localStorage
   */
  static storeAuthData(authData: LoginResponse): void {
    localStorage.setItem('accessToken', authData.accessToken);
    if (authData.refreshToken) {
      localStorage.setItem('refreshToken', authData.refreshToken);
    }
    localStorage.setItem('user', JSON.stringify(authData.user));
  }

  /**
   * Clear all authentication data from localStorage
   */
  static clearAuthData(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  }

  /**
   * Get stored user data from localStorage
   */
  static getStoredUser(): User | null {
    try {
      const userData = localStorage.getItem('user');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  }

  /**
   * Get stored tokens from localStorage
   */
  static getStoredTokens(): { accessToken: string | null; refreshToken: string | null } {
    return {
      accessToken: localStorage.getItem('accessToken'),
      refreshToken: localStorage.getItem('refreshToken'),
    };
  }

  /**
   * Check if user has specific role
   */
  static hasRole(user: User | null, role: string): boolean {
    return user?.roles?.includes(role) || false;
  }

  /**
   * Check if user is admin
   */
  static isAdmin(user?: User | null): boolean {
    const currentUser = user || this.getStoredUser();
    return this.hasRole(currentUser, 'Admin') || this.hasRole(currentUser, 'ServiceIdentity');
  }
}

// Export convenience instance
export const authApi = AuthApi;