export type TaskStatus = 0 | 1 | 2 | 3 | 4;
// 0: Backlog, 1: Todo, 2: InProgress, 3: InReview, 4: Done

export type TaskPriority = 0 | 1 | 2 | 3;
// 0: Low, 1: Medium, 2: High, 3: Urgent

export interface User {
  id: string;
  email: string;
  fullName: string;
  avatarUrl?: string;
}

export interface Organization {
  id: string;
  name: string;
  slug: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  user: User;
  organization: Organization;
}

export interface Project {
  id: string;
  name: string;
  key: string;
  description?: string;
  status: number;
  organizationId: string;
  teamId?: string;
  teamName?: string;
  tasksCount: number;
  createdAt: string;
}

export interface TaskItem {
  id: string;
  projectId: string;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId?: string;
  assigneeName?: string;
  creatorId: string;
  creatorName: string;
  dueDate?: string;
  createdAt: string;
  commentsCount: number;
}

export interface CommentItem {
  id: string;
  taskItemId: string;
  authorId: string;
  authorName: string;
  content: string;
  createdAt: string;
}

