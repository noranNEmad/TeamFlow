import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Navbar } from '../components/Navbar';
import { TaskCard } from '../components/TaskCard';
import { CreateTaskModal } from '../components/CreateTaskModal';
import { TaskDetailsModal } from '../components/TaskDetailsModal';
import { projectsApi } from '../api/projects';
import { tasksApi } from '../api/tasks';
import { Project, TaskItem, TaskStatus } from '../types';
import { Plus, ArrowLeft, CheckCircle2, Circle, Clock, CheckCheck } from 'lucide-react';

interface ColumnDef {
  status: TaskStatus;
  title: string;
  icon: React.ReactNode;
  bgBadge: string;
}

const columns: ColumnDef[] = [
  { status: 1, title: 'To Do', icon: <Circle className="w-3.5 h-3.5 text-slate-400" />, bgBadge: 'bg-slate-100 text-slate-700' },
  { status: 2, title: 'In Progress', icon: <Clock className="w-3.5 h-3.5 text-blue-500" />, bgBadge: 'bg-blue-50 text-blue-700' },
  { status: 3, title: 'In Review', icon: <CheckCircle2 className="w-3.5 h-3.5 text-purple-500" />, bgBadge: 'bg-purple-50 text-purple-700' },
  { status: 4, title: 'Done', icon: <CheckCheck className="w-3.5 h-3.5 text-emerald-500" />, bgBadge: 'bg-emerald-50 text-emerald-700' },
];

export const KanbanPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState(true);

  // Modals
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedColumnStatus, setSelectedColumnStatus] = useState<TaskStatus>(1);
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);

  useEffect(() => {
    if (id) {
      loadProjectAndTasks(id);
    }
  }, [id]);

  const loadProjectAndTasks = async (projectId: string) => {
    setLoading(true);
    try {
      const [projData, tasksData] = await Promise.all([
        projectsApi.getById(projectId),
        projectsApi.getTasks(projectId),
      ]);
      setProject(projData);
      setTasks(tasksData);
    } catch (err) {
      console.error('Failed to load project board', err);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (taskId: string, newStatus: number) => {
    try {
      // Optimistic update
      setTasks((prev) =>
        prev.map((t) => (t.id === taskId ? { ...t, status: newStatus as TaskStatus } : t))
      );
      await tasksApi.updateStatus(taskId, newStatus as TaskStatus);
    } catch (err) {
      console.error('Failed to update status', err);
      if (id) loadProjectAndTasks(id);
    }
  };

  const handleTaskCreated = (newTask: TaskItem) => {
    setTasks((prev) => [newTask, ...prev]);
  };

  const handleTaskUpdated = (updatedTask: TaskItem) => {
    setTasks((prev) => prev.map((t) => (t.id === updatedTask.id ? updatedTask : t)));
    setSelectedTask(updatedTask);
  };

  const handleTaskDeleted = (taskId: string) => {
    setTasks((prev) => prev.filter((t) => t.id !== taskId));
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <Navbar />

      <main className="flex-1 max-w-[1600px] w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 flex flex-col">
        {/* Top Navigation & Project Info */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
          <div className="flex items-center gap-3">
            <Link
              to="/projects"
              className="p-2 rounded-xl bg-white border border-slate-200 text-slate-500 hover:text-slate-900 hover:border-slate-300 transition-colors shadow-sm"
              title="Back to Projects"
            >
              <ArrowLeft className="w-4 h-4" />
            </Link>

            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded uppercase">
                  {project?.key || '...'}
                </span>
                <h1 className="text-xl font-extrabold text-slate-900 tracking-tight">
                  {project?.name || 'Loading Project...'}
                </h1>
              </div>
              {project?.description && (
                <p className="text-xs text-slate-500 mt-0.5">{project.description}</p>
              )}
            </div>
          </div>

          <button
            onClick={() => {
              setSelectedColumnStatus(1);
              setIsCreateModalOpen(true);
            }}
            className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-semibold shadow-sm shadow-blue-500/30 transition-colors self-start sm:self-auto"
          >
            <Plus className="w-4 h-4" />
            <span>Create Task</span>
          </button>
        </div>

        {/* Kanban Board Grid */}
        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 flex-1 items-start animate-pulse">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-96 bg-slate-200/60 rounded-2xl border border-slate-200/80" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 flex-1 items-start">
            {columns.map((col) => {
              const colTasks = tasks.filter((t) => t.status === col.status);

              return (
                <div
                  key={col.status}
                  className="bg-slate-100/70 border border-slate-200/80 rounded-2xl p-3.5 flex flex-col max-h-[calc(100vh-180px)]"
                >
                  {/* Column Header */}
                  <div className="flex items-center justify-between pb-3 px-1">
                    <div className="flex items-center gap-2">
                      {col.icon}
                      <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider">{col.title}</h3>
                      <span className={`text-[11px] font-bold px-1.5 py-0.2 rounded-md ${col.bgBadge}`}>
                        {colTasks.length}
                      </span>
                    </div>

                    <button
                      onClick={() => {
                        setSelectedColumnStatus(col.status);
                        setIsCreateModalOpen(true);
                      }}
                      className="p-1 rounded-md text-slate-400 hover:text-blue-600 hover:bg-white transition-colors"
                      title={`Add task to ${col.title}`}
                    >
                      <Plus className="w-4 h-4" />
                    </button>
                  </div>

                  {/* Cards Container */}
                  <div className="space-y-3 overflow-y-auto pr-1 flex-1">
                    {colTasks.map((task) => (
                      <TaskCard
                        key={task.id}
                        task={task}
                        projectKey={project?.key || 'TASK'}
                        onClick={() => {
                          setSelectedTask(task);
                          setIsDetailsModalOpen(true);
                        }}
                        onStatusChange={(newStatus) => handleStatusChange(task.id, newStatus)}
                      />
                    ))}

                    {colTasks.length === 0 && (
                      <div className="py-8 text-center border-2 border-dashed border-slate-200 rounded-xl text-xs text-slate-400">
                        No tasks in {col.title}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </main>

      {/* Modals */}
      {id && (
        <CreateTaskModal
          isOpen={isCreateModalOpen}
          projectId={id}
          defaultStatus={selectedColumnStatus}
          onClose={() => setIsCreateModalOpen(false)}
          onCreated={handleTaskCreated}
        />
      )}

      <TaskDetailsModal
        isOpen={isDetailsModalOpen}
        task={selectedTask}
        projectKey={project?.key || 'TASK'}
        onClose={() => setIsDetailsModalOpen(false)}
        onTaskUpdated={handleTaskUpdated}
        onTaskDeleted={handleTaskDeleted}
      />
    </div>
  );
};
