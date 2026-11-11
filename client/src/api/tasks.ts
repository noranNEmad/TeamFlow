import { apiClient } from './client';
import { TaskItem, CommentItem, TaskStatus, TaskPriority } from '../types';

export const tasksApi = {
  create: async (data: {
    projectId: string;
    title: string;
    description?: string;
    status: TaskStatus;
    priority: TaskPriority;
    dueDate?: string;
  }): Promise<TaskItem> => {
    const res = await apiClient.post<TaskItem>('/tasks', data);
    return res.data;
  },

  updateStatus: async (taskId: string, status: TaskStatus): Promise<TaskItem> => {
    const res = await apiClient.patch<TaskItem>(`/tasks/${taskId}/status`, { status });
    return res.data;
  },

  delete: async (taskId: string): Promise<void> => {
    await apiClient.delete(`/tasks/${taskId}`);
  },

  addComment: async (taskId: string, content: string): Promise<CommentItem> => {
    const res = await apiClient.post<CommentItem>(`/tasks/${taskId}/comments`, { content });
    return res.data;
  },

  getComments: async (taskId: string): Promise<CommentItem[]> => {
    const res = await apiClient.get<CommentItem[]>(`/tasks/${taskId}/comments`);
    return res.data;
  },
};

