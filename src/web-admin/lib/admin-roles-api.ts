import { authApi } from './api';

export interface AdminRoleSummary {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  assignedScopeCount: number;
  assignedScopes: string[];
}

export interface AdminRoleDetail {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  assignedScopes: string[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface AvailableScope {
  name: string;
  displayName: string;
  description: string;
  group: string;
  resources: string[];
}

export interface CreateRolePayload {
  name: string;
  description?: string;
  isActive?: boolean;
}

export interface UpdateRolePayload {
  description?: string;
  isActive: boolean;
}

export interface UpdateRoleScopesPayload {
  scopeNames: string[];
}

export const adminRolesApi = {
  async listRoles(): Promise<AdminRoleSummary[]> {
    const response = await authApi.get<AdminRoleSummary[]>('/api/admin/roles');
    return response.data;
  },

  async getRole(roleId: string): Promise<AdminRoleDetail> {
    const response = await authApi.get<AdminRoleDetail>(`/api/admin/roles/${roleId}`);
    return response.data;
  },

  async getAvailableScopes(): Promise<AvailableScope[]> {
    const response = await authApi.get<AvailableScope[]>('/api/admin/roles/available-scopes');
    return response.data;
  },

  async createRole(payload: CreateRolePayload): Promise<AdminRoleDetail> {
    const response = await authApi.post<AdminRoleDetail>('/api/admin/roles', payload);
    return response.data;
  },

  async updateRole(roleId: string, payload: UpdateRolePayload): Promise<AdminRoleDetail> {
    const response = await authApi.put<AdminRoleDetail>(`/api/admin/roles/${roleId}`, payload);
    return response.data;
  },

  async updateRoleScopes(roleId: string, payload: UpdateRoleScopesPayload): Promise<AdminRoleDetail> {
    const response = await authApi.put<AdminRoleDetail>(`/api/admin/roles/${roleId}/scopes`, payload);
    return response.data;
  },
};
