import { apiClient } from './client';
import { Project, TaskItem } from '../types';

export const projectsApi = {
  getAll: async (): Promise<Project[]> => {
    const res = await apiClient.get<Project[]>('/projects');
    return res.data;
  },

  getById: async (id: string): Promise<Project> => {
    const res = await apiClient.get<Project>(`/projects/${id}`);
    return res.data;
  },

  create: async (data: { name: string; key: string; description?: string }): Promise<Project> => {
    const res = await apiClient.post<Project>('/projects', data);
    return res.data;
  },

  getTasks: async (projectId: string, status?: number): Promise<TaskItem[]> => {
    const params = status !== undefined ? { status } : {};
    const res = await apiClient.get<TaskItem[]>(`/projects/${projectId}/tasks`, { params });
    return res.data;
  },
};

