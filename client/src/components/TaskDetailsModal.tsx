import React, { useState, useEffect } from 'react';
import { X, Calendar, MessageSquare, Send, Trash2, Clock } from 'lucide-react';
import { tasksApi } from '../api/tasks';
import { TaskItem, CommentItem, TaskStatus } from '../types';

interface TaskDetailsModalProps {
  task: TaskItem | null;
  projectKey: string;
  isOpen: boolean;
  onClose: () => void;
  onTaskUpdated: (updatedTask: TaskItem) => void;
  onTaskDeleted: (taskId: string) => void;
}

export const TaskDetailsModal: React.FC<TaskDetailsModalProps> = ({
  task,
  projectKey,
  isOpen,
  onClose,
  onTaskUpdated,
  onTaskDeleted,
}) => {
  const [comments, setComments] = useState<CommentItem[]>([]);
  const [newComment, setNewComment] = useState('');
  const [commentLoading, setCommentLoading] = useState(false);
  const [statusLoading, setStatusLoading] = useState(false);

  useEffect(() => {
    if (task && isOpen) {
      loadComments(task.id);
    }
  }, [task, isOpen]);

  const loadComments = async (taskId: string) => {
    try {
      const data = await tasksApi.getComments(taskId);
      setComments(data);
    } catch (err) {
      console.error('Failed to load comments', err);
    }
  };

  if (!isOpen || !task) return null;

  const handleStatusChange = async (newStatus: TaskStatus) => {
    setStatusLoading(true);
    try {
      const updated = await tasksApi.updateStatus(task.id, newStatus);
      onTaskUpdated(updated);
    } catch (err) {
      console.error('Failed to update status', err);
    } finally {
      setStatusLoading(false);
    }
  };

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    setCommentLoading(true);
    try {
      const comment = await tasksApi.addComment(task.id, newComment.trim());
      setComments((prev) => [...prev, comment]);
      setNewComment('');
      onTaskUpdated({ ...task, commentsCount: task.commentsCount + 1 });
    } catch (err) {
      console.error('Failed to add comment', err);
    } finally {
      setCommentLoading(false);
    }
  };

  const handleDelete = async () => {
    if (window.confirm('Are you sure you want to delete this task?')) {
      try {
        await tasksApi.delete(task.id);
        onTaskDeleted(task.id);
        onClose();
      } catch (err) {
        console.error('Failed to delete task', err);
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 max-w-2xl w-full max-h-[90vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="text-xs font-mono font-bold text-blue-600 bg-blue-50 px-2.5 py-1 rounded-md uppercase">
              {projectKey}-{task.id.slice(0, 4)}
            </span>
            <select
              value={task.status}
              disabled={statusLoading}
              onChange={(e) => handleStatusChange(Number(e.target.value) as TaskStatus)}
              className="text-xs font-semibold bg-slate-100 border-none rounded-lg px-2.5 py-1 text-slate-700 hover:bg-slate-200 focus:ring-2 focus:ring-blue-500 cursor-pointer"
            >
              <option value={0}>Backlog</option>
              <option value={1}>Todo</option>
              <option value={2}>In Progress</option>
              <option value={3}>In Review</option>
              <option value={4}>Done</option>
            </select>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={handleDelete}
              className="text-slate-400 hover:text-rose-600 p-1.5 rounded-lg hover:bg-rose-50 transition-colors"
              title="Delete Task"
            >
              <Trash2 className="w-4 h-4" />
            </button>
            <button onClick={onClose} className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100">
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Content Body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1">
          <div>
            <h3 className="text-lg font-bold text-slate-900 mb-2">{task.title}</h3>
            {task.description ? (
              <p className="text-sm text-slate-600 whitespace-pre-wrap leading-relaxed">{task.description}</p>
            ) : (
              <p className="text-sm text-slate-400 italic">No description provided.</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4 py-3 border-y border-slate-100 text-xs">
            <div className="flex items-center gap-2 text-slate-600">
              <Clock className="w-4 h-4 text-slate-400" />
              <span>Created by <strong>{task.creatorName}</strong> on {new Date(task.createdAt).toLocaleDateString()}</span>
            </div>
            {task.dueDate && (
              <div className="flex items-center gap-2 text-slate-600">
                <Calendar className="w-4 h-4 text-slate-400" />
                <span>Due Date: <strong>{new Date(task.dueDate).toLocaleDateString()}</strong></span>
              </div>
            )}
          </div>

          {/* Comments Section */}
          <div className="space-y-4">
            <div className="flex items-center gap-2 font-semibold text-slate-800 text-sm">
              <MessageSquare className="w-4 h-4 text-blue-600" />
              <span>Discussion & Comments ({comments.length})</span>
            </div>

            <div className="space-y-3">
              {comments.map((comment) => (
                <div key={comment.id} className="p-3 bg-slate-50 border border-slate-100 rounded-xl space-y-1">
                  <div className="flex items-center justify-between text-[11px] text-slate-500">
                    <span className="font-bold text-slate-700">{comment.authorName}</span>
                    <span>{new Date(comment.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                  </div>
                  <p className="text-sm text-slate-800">{comment.content}</p>
                </div>
              ))}

              {comments.length === 0 && (
                <p className="text-xs text-slate-400 italic py-2">No comments yet. Start the conversation!</p>
              )}
            </div>

            {/* Add Comment Box */}
            <form onSubmit={handleAddComment} className="flex gap-2 pt-2">
              <input
                type="text"
                placeholder="Write a comment..."
                value={newComment}
                onChange={(e) => setNewComment(e.target.value)}
                className="flex-1 px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white"
              />
              <button
                type="submit"
                disabled={commentLoading || !newComment.trim()}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-sm font-semibold flex items-center gap-1.5 disabled:opacity-50 transition-colors"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Send</span>
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};
