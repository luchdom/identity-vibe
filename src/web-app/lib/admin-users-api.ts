import { authApi } from './api';

export interface AdminUserSummary {
  id: string;
  email: string;
  userName: string;
  phoneNumber?: string | null;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminUserDetail {
  id: string;
  email: string;
  userName: string;
  phoneNumber?: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: string[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminRoleOption {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  assignedScopes: string[];
}

export interface AdminUserListResponse {
  users: AdminUserSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ListUsersQuery {
  search?: string;
  isActive?: boolean;
  role?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateUserPayload {
  email: string;
  password: string;
  phoneNumber?: string;
  isActive?: boolean;
  roleNames: string[];
}

export interface UpdateUserPayload {
  email?: string;
  phoneNumber?: string;
}

export interface UpdateUserRolesPayload {
  roleNames: string[];
}

export interface UpdateUserStatusPayload {
  isActive: boolean;
}

export interface ResetUserPasswordPayload {
  newPassword: string;
}

export const adminUsersApi = {
  async listUsers(query: ListUsersQuery = {}): Promise<AdminUserListResponse> {
    const response = await authApi.get<AdminUserListResponse>('/api/admin/users', {
      params: query,
    });
    return response.data;
  },

  async getUser(userId: string): Promise<AdminUserDetail> {
    const response = await authApi.get<AdminUserDetail>(`/api/admin/users/${userId}`);
    return response.data;
  },

  async getAvailableRoles(): Promise<AdminRoleOption[]> {
    const response = await authApi.get<AdminRoleOption[]>('/api/admin/users/available-roles');
    return response.data;
  },

  async createUser(payload: CreateUserPayload): Promise<AdminUserDetail> {
    const response = await authApi.post<AdminUserDetail>('/api/admin/users', payload);
    return response.data;
  },

  async updateUser(userId: string, payload: UpdateUserPayload): Promise<AdminUserDetail> {
    const response = await authApi.put<AdminUserDetail>(`/api/admin/users/${userId}`, payload);
    return response.data;
  },

  async updateUserRoles(userId: string, payload: UpdateUserRolesPayload): Promise<AdminUserDetail> {
    const response = await authApi.put<AdminUserDetail>(`/api/admin/users/${userId}/roles`, payload);
    return response.data;
  },

  async updateUserStatus(userId: string, payload: UpdateUserStatusPayload): Promise<AdminUserDetail> {
    const response = await authApi.put<AdminUserDetail>(`/api/admin/users/${userId}/status`, payload);
    return response.data;
  },

  async resetUserPassword(userId: string, payload: ResetUserPasswordPayload): Promise<AdminUserDetail> {
    const response = await authApi.put<AdminUserDetail>(`/api/admin/users/${userId}/password`, payload);
    return response.data;
  },
};
