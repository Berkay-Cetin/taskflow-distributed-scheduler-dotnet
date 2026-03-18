import { useState, useEffect } from 'react'
import { Plus, Activity, RefreshCw, ChevronRight, Clock, CheckCircle, XCircle, Loader, Filter } from 'lucide-react'
import type { ScheduledTask, TaskExecution, ExecStatus } from './types'
import { taskApi } from './api/taskApi'
import { TaskCard } from './components/TaskCard'
import { TaskForm } from './components/TaskForm'
import { StatusBadge } from './components/StatusBadge'
import { StatsDashboard } from './components/StatsDashboard'

function duration(ms: number) {
  if (!ms) return '-'
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

const PAGE_SIZE = 20

export default function App() {
  const [tasks, setTasks]                 = useState<ScheduledTask[]>([])
  const [selected, setSelected]           = useState<ScheduledTask | null>(null)
  const [showForm, setShowForm]           = useState(false)
  const [loading, setLoading]             = useState(false)
  const [allExecutions, setAllExecutions] = useState<(TaskExecution & { taskName: string })[]>([])
  const [statusFilter, setStatusFilter]   = useState<ExecStatus | 'All'>('All')
  const [taskFilter, setTaskFilter]       = useState<string | null>(null)
  const [page, setPage]                   = useState(1)
  const [activeTab, setActiveTab]         = useState<'tasks' | 'stats'>('tasks')

  const loadTasks = async () => {
    setLoading(true)
    try {
      const data = await taskApi.getAll()
      setTasks(data)

      const execPromises = data.map(t =>
        taskApi.getExecutions(t.id, 1).then(execs =>
          execs.map(e => ({ ...e, taskName: t.name }))
        )
      )
      const all = (await Promise.all(execPromises)).flat()
      all.sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime())
      setAllExecutions(all)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadTasks() }, [])

  useEffect(() => {
    const interval = setInterval(loadTasks, 5000)
    return () => clearInterval(interval)
  }, [])

  const handleSelectTask = (task: ScheduledTask) => {
    if (selected?.id === task.id) {
      setSelected(null)
      setTaskFilter(null)
    } else {
      setSelected(task)
      setTaskFilter(task.id)
      setPage(1)
    }
  }

  const stats = {
    total  : tasks.length,
    enabled: tasks.filter(t => t.isEnabled).length,
    running: tasks.filter(t => t.lastStatus === 'Running').length,
    failed : tasks.filter(t => t.lastStatus === 'Failed').length,
  }

  const filtered = allExecutions
    .filter(e => statusFilter === 'All' || e.status === statusFilter)
    .filter(e => !taskFilter || e.taskId === taskFilter)

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  const statusFilters: (ExecStatus | 'All')[] = ['All', 'Running', 'Success', 'Failed', 'Retrying', 'Dead']

  const filterColors: Record<string, string> = {
    All     : 'bg-slate-700 text-slate-200',
    Running : 'bg-blue-900 text-blue-300',
    Success : 'bg-green-900 text-green-300',
    Failed  : 'bg-red-900 text-red-300',
    Retrying: 'bg-yellow-900 text-yellow-300',
    Dead    : 'bg-red-950 text-red-400',
  }

  return (
    <div className="min-h-screen bg-dark-900">
      {/* Header */}
      <header className="border-b border-dark-700 px-6 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Activity className="text-blue-400" size={24} />
            <h1 className="text-xl font-bold text-white">TaskFlow</h1>
            <span className="text-xs text-slate-500 bg-dark-700 px-2 py-0.5 rounded">
              Distributed Task Scheduler
            </span>
          </div>
          <div className="flex items-center gap-3">
            <button onClick={loadTasks}
              className="p-2 text-slate-400 hover:text-white transition-colors">
              <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
            </button>
            <button onClick={() => setShowForm(true)}
              className="flex items-center gap-2 bg-accent-500 hover:bg-blue-700
                         text-white px-4 py-2 rounded text-sm font-medium transition-colors">
              <Plus size={16} /> New Task
            </button>
          </div>
        </div>
      </header>

      {/* Tab Navigation */}
      <div className="border-b border-dark-700 px-6">
        <div className="max-w-7xl mx-auto flex gap-6">
          {(['tasks', 'stats'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`py-3 text-sm font-medium border-b-2 transition-colors
                ${activeTab === tab
                  ? 'border-blue-500 text-blue-400'
                  : 'border-transparent text-slate-400 hover:text-white'}`}
            >
              {tab === 'tasks' ? '⚡ Tasks' : '📊 Statistics'}
            </button>
          ))}
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        {activeTab === 'stats' ? (
          <StatsDashboard />
        ) : (
          <>
            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
              {[
                { label: 'Total Tasks', value: stats.total,   color: 'text-white' },
                { label: 'Enabled',     value: stats.enabled, color: 'text-green-400' },
                { label: 'Running',     value: stats.running, color: 'text-blue-400' },
                { label: 'Failed',      value: stats.failed,  color: 'text-red-400' },
              ].map(s => (
                <div key={s.label} className="bg-dark-800 border border-dark-600 rounded-lg p-4">
                  <p className="text-slate-400 text-xs mb-1">{s.label}</p>
                  <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
                </div>
              ))}
            </div>

            {/* Create Form */}
            {showForm && (
              <div className="bg-dark-800 border border-dark-600 rounded-lg p-5">
                <h2 className="text-white font-semibold mb-4">Create New Task</h2>
                <TaskForm
                  onCreated={() => { setShowForm(false); loadTasks() }}
                  onCancel={() => setShowForm(false)}
                />
              </div>
            )}

            {/* Tasks */}
            <div className="flex-1">
              {tasks.length > 0 && !selected && (
                <div className="flex items-center gap-2 text-slate-500 text-xs mb-3
                                bg-dark-800 border border-dark-700 rounded-lg px-4 py-2.5">
                  <ChevronRight size={13} className="text-blue-400" />
                  <span>Click on a task card to filter the execution table below</span>
                </div>
              )}

              {selected && (
                <div className="flex items-center gap-2 text-xs mb-3
                                bg-blue-950/40 border border-blue-800/50 rounded-lg px-4 py-2.5">
                  <Filter size={13} className="text-blue-400" />
                  <span className="text-blue-300">
                    Showing executions for <strong>{selected.name}</strong>
                  </span>
                  <button
                    onClick={() => { setSelected(null); setTaskFilter(null) }}
                    className="ml-auto text-blue-400 hover:text-white transition-colors"
                  >
                    Clear filter ×
                  </button>
                </div>
              )}

              {tasks.length === 0 && !loading && (
                <div className="text-center py-16 text-slate-500">
                  <Activity size={40} className="mx-auto mb-3 opacity-30" />
                  <p>No tasks yet. Create your first task!</p>
                </div>
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {tasks.map(task => (
                  <div key={task.id}
                    className={`rounded-lg transition-all duration-200 ring-2
                      ${selected?.id === task.id ? 'ring-blue-500' : 'ring-transparent'}`}
                  >
                    <TaskCard
                      task={task}
                      onSelect={handleSelectTask}
                      onRefresh={loadTasks}
                    />
                  </div>
                ))}
              </div>
            </div>

            {/* Global History Table */}
            <div className="bg-dark-800 border border-dark-600 rounded-lg overflow-hidden">
              <div className="flex items-center justify-between px-5 py-4 border-b border-dark-600 flex-wrap gap-3">
                <div className="flex items-center gap-2">
                  <Clock size={16} className="text-blue-400" />
                  <h2 className="text-white font-semibold">Execution History</h2>
                  <span className="text-xs text-slate-500 bg-dark-700 px-2 py-0.5 rounded">
                    {filtered.length} records
                  </span>
                </div>

                <div className="flex items-center gap-2 flex-wrap">
                  {statusFilters.map(f => (
                    <button
                      key={f}
                      onClick={() => { setStatusFilter(f); setPage(1) }}
                      className={`px-2.5 py-1 rounded text-xs font-medium transition-colors
                        ${statusFilter === f
                          ? filterColors[f]
                          : 'bg-dark-700 text-slate-400 hover:bg-dark-600'}`}
                    >
                      {f}
                      {f !== 'All' && (
                        <span className="ml-1 opacity-70">
                          ({allExecutions.filter(e =>
                            e.status === f && (!taskFilter || e.taskId === taskFilter)
                          ).length})
                        </span>
                      )}
                    </button>
                  ))}
                  <button onClick={loadTasks}
                    className="text-slate-500 hover:text-white transition-colors ml-1">
                    <RefreshCw size={13} className={loading ? 'animate-spin' : ''} />
                  </button>
                </div>
              </div>

              {paginated.length === 0 ? (
                <div className="text-center py-10 text-slate-500">
                  <Clock size={28} className="mx-auto mb-2 opacity-30" />
                  <p className="text-sm">No executions found</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-dark-600 text-slate-400 text-xs">
                        <th className="text-left px-5 py-3 font-medium">Task</th>
                        <th className="text-left px-4 py-3 font-medium">Status</th>
                        <th className="text-left px-4 py-3 font-medium">HTTP</th>
                        <th className="text-left px-4 py-3 font-medium">Duration</th>
                        <th className="text-left px-4 py-3 font-medium">Attempt</th>
                        <th className="text-left px-4 py-3 font-medium">Started</th>
                        <th className="text-left px-4 py-3 font-medium">Error</th>
                      </tr>
                    </thead>
                    <tbody>
                      {paginated.map((exec, i) => (
                        <tr key={exec.id}
                          className={`border-b border-dark-700 hover:bg-dark-700/60 transition-colors
                            ${i % 2 === 0 ? '' : 'bg-dark-900/20'}`}
                        >
                          <td className="px-5 py-3">
                            <div className="flex items-center gap-2">
                              {exec.status === 'Success'
                                ? <CheckCircle size={13} className="text-green-400 shrink-0" />
                                : exec.status === 'Running' || exec.status === 'Retrying'
                                ? <Loader size={13} className="text-blue-400 animate-spin shrink-0" />
                                : <XCircle size={13} className="text-red-400 shrink-0" />}
                              <span
                                className="text-white font-medium cursor-pointer hover:text-blue-400 transition-colors"
                                onClick={() => {
                                  const t = tasks.find(t => t.id === exec.taskId)
                                  if (t) handleSelectTask(t)
                                }}
                              >
                                {exec.taskName}
                              </span>
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            <StatusBadge status={exec.status} />
                          </td>
                          <td className="px-4 py-3">
                            {exec.statusCode ? (
                              <span className={`text-xs font-mono px-1.5 py-0.5 rounded
                                ${exec.statusCode >= 200 && exec.statusCode < 300
                                  ? 'bg-green-900/40 text-green-400'
                                  : 'bg-red-900/40 text-red-400'}`}>
                                {exec.statusCode}
                              </span>
                            ) : <span className="text-slate-600">—</span>}
                          </td>
                          <td className="px-4 py-3 text-slate-300 font-mono text-xs">
                            {duration(exec.durationMs)}
                          </td>
                          <td className="px-4 py-3">
                            {exec.attemptNo > 1
                              ? <span className="text-xs text-yellow-400">#{exec.attemptNo}</span>
                              : <span className="text-slate-600 text-xs">#1</span>}
                          </td>
                          <td className="px-4 py-3 text-slate-400 text-xs">
                            <div>{timeAgo(exec.startedAt)}</div>
                            <div className="text-slate-600">{new Date(exec.startedAt).toLocaleTimeString()}</div>
                          </td>
                          <td className="px-4 py-3 max-w-xs">
                            {exec.errorMessage ? (
                              <span className="text-xs text-red-400 font-mono truncate block max-w-48"
                                title={exec.errorMessage}>
                                {exec.errorMessage}
                              </span>
                            ) : <span className="text-slate-600 text-xs">—</span>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {totalPages > 1 && (
                <div className="flex items-center justify-between px-5 py-3 border-t border-dark-600">
                  <span className="text-xs text-slate-500">
                    Page {page} of {totalPages} · {filtered.length} total
                  </span>
                  <div className="flex gap-2">
                    <button
                      disabled={page === 1}
                      onClick={() => setPage(p => Math.max(1, p - 1))}
                      className="px-3 py-1.5 text-xs bg-dark-700 hover:bg-dark-600
                                 disabled:opacity-30 rounded transition-colors text-slate-300"
                    >
                      ← Prev
                    </button>
                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                      const p = Math.max(1, Math.min(page - 2, totalPages - 4)) + i
                      return (
                        <button
                          key={p}
                          onClick={() => setPage(p)}
                          className={`px-3 py-1.5 text-xs rounded transition-colors
                            ${page === p
                              ? 'bg-accent-500 text-white'
                              : 'bg-dark-700 hover:bg-dark-600 text-slate-300'}`}
                        >
                          {p}
                        </button>
                      )
                    })}
                    <button
                      disabled={page === totalPages}
                      onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                      className="px-3 py-1.5 text-xs bg-dark-700 hover:bg-dark-600
                                 disabled:opacity-30 rounded transition-colors text-slate-300"
                    >
                      Next →
                    </button>
                  </div>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}