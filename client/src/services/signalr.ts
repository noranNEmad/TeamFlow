import * as signalR from '@microsoft/signalr';
import { TaskItem, CommentItem } from '../types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;

  public async startConnection(token: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/teamflow', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    try {
      await this.connection.start();
    } catch (err) {
      console.warn('SignalR Connection Error:', err);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  public async joinProjectBoard(projectId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinProjectBoard', projectId);
    }
  }

  public async leaveProjectBoard(projectId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveProjectBoard', projectId);
    }
  }

  public onTaskCreated(callback: (task: TaskItem) => void): () => void {
    this.connection?.on('TaskCreated', callback);
    return () => this.connection?.off('TaskCreated', callback);
  }

  public onTaskStatusUpdated(callback: (data: { taskId: string; status: number }) => void): () => void {
    this.connection?.on('TaskStatusUpdated', callback);
    return () => this.connection?.off('TaskStatusUpdated', callback);
  }

  public onCommentAdded(callback: (data: { taskId: string; comment: CommentItem }) => void): () => void {
    this.connection?.on('CommentAdded', callback);
    return () => this.connection?.off('CommentAdded', callback);
  }
}

export const signalRService = new SignalRService();

