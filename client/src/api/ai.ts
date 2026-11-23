import { apiClient } from './client';

export interface TaskAiBreakdownResponse {
  formattedMarkdown: string;
  suggestedPriority: number;
  acceptanceCriteria: string[];
  summary: string;
}

export const aiApi = {
  generateBreakdown: async (title: string, context?: string): Promise<TaskAiBreakdownResponse> => {
    const res = await apiClient.post<TaskAiBreakdownResponse>('/ai/task-breakdown', {
      title,
      context,
    });
    return res.data;
  },

  summarizeComments: async (comments: string[]): Promise<string> => {
    const res = await apiClient.post<{ summary: string }>('/ai/summarize-discussion', {
      comments,
    });
    return res.data.summary;
  },
};
