import React, { useState } from 'react';
import { X, CheckSquare, Sparkles } from 'lucide-react';
import { tasksApi } from '../api/tasks';
import { aiApi } from '../api/ai';
import { TaskItem, TaskStatus, TaskPriority } from '../types';

interface CreateTaskModalProps {
  isOpen: boolean;
  projectId: string;
  defaultStatus?: TaskStatus;
  onClose: () => void;
  onCreated: (task: TaskItem) => void;
}

export const CreateTaskModal: React.FC<CreateTaskModalProps> = ({
  isOpen,
  projectId,
  defaultStatus = 1,
  onClose,
  onCreated,
}) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState<TaskStatus>(defaultStatus);
  const [priority, setPriority] = useState<TaskPriority>(1);
  const [dueDate, setDueDate] = useState('');
  const [loading, setLoading] = useState(false);
  const [aiLoading, setAiLoading] = useState(false);
  const [error, setError] = useState('');

  if (!isOpen) return null;

  const handleAiGenerate = async () => {
    if (!title.trim()) return;
    setAiLoading(true);
    setError('');
    try {
      const breakdown = await aiApi.generateBreakdown(title, description);
      setDescription(breakdown.formattedMarkdown);
      if (breakdown.suggestedPriority !== undefined) {
        setPriority(breakdown.suggestedPriority as TaskPriority);
      }
    } catch (err) {
      console.warn('AI Generate backend failed, using client fallback', err);
      const fallbackText = `### 🎯 User Story & Scope\nImplement and verify full-stack requirements for '${title}'.\n\n### ✅ Acceptance Criteria\n- [ ] Implement core domain logic and validation for '${title}'\n- [ ] Expose RESTful API endpoints with standard HTTP status codes\n- [ ] Connect reactive frontend state with optimistic UI updates\n- [ ] Add automated unit and integration tests\n\n### 🧪 Verification\n- [ ] Automated integration tests\n- [ ] Manual regression testing`;
      setDescription(fallbackText);
    } finally {
      setAiLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const task = await tasksApi.create({
        projectId,
        title,
        description: description || undefined,
        status,
        priority,
        dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
      });
      onCreated(task);
      onClose();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create task');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 max-w-lg w-full overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-2 font-bold text-slate-800">
            <CheckSquare className="w-5 h-5 text-blue-600" />
            <span>New Task</span>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600 rounded-lg p-1">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-sm rounded-xl">
              {error}
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">Task Title</label>
            <input
              type="text"
              required
              placeholder="e.g. Implement user registration endpoint"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white"
            />
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-semibold text-slate-700 uppercase">Description (Markdown Supported)</label>
              <button
                type="button"
                onClick={handleAiGenerate}
                disabled={aiLoading || !title.trim()}
                className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium text-purple-700 bg-purple-50 hover:bg-purple-100 border border-purple-200/80 rounded-lg transition-all duration-150 disabled:opacity-40 shadow-sm"
                title="Use AI to generate acceptance criteria and scope breakdown"
              >
                <Sparkles className={`w-3.5 h-3.5 text-purple-600 ${aiLoading ? 'animate-spin' : ''}`} />
                <span>{aiLoading ? 'AI Generating...' : '✨ AI Copilot'}</span>
              </button>
            </div>
            <textarea
              rows={4}
              placeholder="Detailed acceptance criteria or implementation notes (or click ✨ AI Copilot to generate)..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:bg-white font-mono text-xs leading-relaxed"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">Initial Status</label>
              <select
                value={status}
                onChange={(e) => setStatus(Number(e.target.value) as TaskStatus)}
                className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={0}>Backlog</option>
                <option value={1}>Todo</option>
                <option value={2}>In Progress</option>
                <option value={3}>In Review</option>
                <option value={4}>Done</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(Number(e.target.value) as TaskPriority)}
                className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={0}>Low</option>
                <option value={1}>Medium</option>
                <option value={2}>High</option>
                <option value={3}>Urgent</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">Due Date</label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-sm shadow-blue-500/30 disabled:opacity-50 transition-colors"
            >
              {loading ? 'Creating...' : 'Create Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

