import React from 'react';
import { TaskItem, TaskPriority } from '../types';
import { MessageSquare, Calendar } from 'lucide-react';

interface TaskCardProps {
  task: TaskItem;
  projectKey: string;
  onClick: () => void;
  onStatusChange: (newStatus: number) => void;
}

const priorityColors: Record<TaskPriority, { bg: string; text: string; label: string }> = {
  0: { bg: 'bg-slate-100', text: 'text-slate-600', label: 'Low' },
  1: { bg: 'bg-blue-50', text: 'text-blue-700', label: 'Medium' },
  2: { bg: 'bg-amber-50', text: 'text-amber-700', label: 'High' },
  3: { bg: 'bg-rose-50', text: 'text-rose-700', label: 'Urgent' },
};

export const TaskCard: React.FC<TaskCardProps> = ({ task, projectKey, onClick, onStatusChange }) => {
  const priorityInfo = priorityColors[task.priority] || priorityColors[1];

  return (
    <div
      onClick={onClick}
      className="bg-white p-3.5 rounded-xl border border-slate-200/90 shadow-sm hover:shadow-md hover:border-blue-400 transition-all cursor-pointer group select-none"
    >
      <div className="flex items-center justify-between gap-2 mb-2">
        <span className="text-xs font-mono font-semibold text-slate-500 uppercase">
          {projectKey}-{task.id.slice(0, 4)}
        </span>
        <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-md ${priorityInfo.bg} ${priorityInfo.text}`}>
          {priorityInfo.label}
        </span>
      </div>

      <h4 className="text-sm font-semibold text-slate-800 group-hover:text-blue-600 transition-colors line-clamp-2 mb-2">
        {task.title}
      </h4>

      {task.description && (
        <p className="text-xs text-slate-500 line-clamp-2 mb-3">
          {task.description}
        </p>
      )}

      <div className="flex items-center justify-between pt-2 border-t border-slate-100 text-xs text-slate-500">
        <div className="flex items-center gap-3">
          {task.dueDate && (
            <div className="flex items-center gap-1 text-slate-500">
              <Calendar className="w-3.5 h-3.5" />
              <span>{new Date(task.dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</span>
            </div>
          )}

          <div className="flex items-center gap-1">
            <MessageSquare className="w-3.5 h-3.5" />
            <span>{task.commentsCount}</span>
          </div>
        </div>

        <div className="flex items-center gap-1.5" onClick={(e) => e.stopPropagation()}>
          <select
            value={task.status}
            onChange={(e) => onStatusChange(Number(e.target.value))}
            className="text-[11px] font-medium bg-slate-50 border border-slate-200 rounded px-1.5 py-0.5 text-slate-600 hover:border-slate-300 focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
          >
            <option value={0}>Backlog</option>
            <option value={1}>Todo</option>
            <option value={2}>In Progress</option>
            <option value={3}>In Review</option>
            <option value={4}>Done</option>
          </select>
        </div>
      </div>
    </div>
  );
};
