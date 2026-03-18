import { useEffect, useState } from 'react'
import type { ScheduledTask, TaskExecution } from '../types'
import { StatusBadge } from './StatusBadge'
import { taskApi } from '../api/taskApi'
import { useTaskHub } from '../hooks/useTaskHub'
import { Clock, CheckCircle, XCircle, RefreshCw, Skull } from 'lucide-react'

interface Props { task: ScheduledTask }

function duration(ms: number) {
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

const iconMap: Record<string, React.ReactNode> = {
  Success  : <CheckCircle size={14} className="text-green-400" />,
  Failed   : <XCircle    size={14} className="text-red-400" />,
  Running  : <RefreshCw  size={14} className="text-blue-400 animate-spin" />,
  Retrying : <RefreshCw  size={14} className="text-yellow-400 animate-spin" />,
  Dead     : <Skull      size={14} className="text-red-600" />,
}

export function ExecutionList({ task }: Props) {
  const [executions, setExecutions] = useState<TaskExecution[]>([])
  const [page, setPage]             = useState(1)
  const [loading, setLoading]       = useState(false)

  const load = async (p = 1) => {
    setLoading(true)
    try {
      const data = await taskApi.getExecutions(task.id, p)
      setExecutions(data)
      setPage(p)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load(1) }, [task.id])

  useTaskHub(task.id, () => load(1))

  const successCount = executions.filter(e => e.status === 'Success').length
  const failedCount  = executions.filter(e => e.status === 'Failed' || e.status === 'Dead').length
  const avgDuration  = executions.length
    ? Math.round(executions.reduce((s, e) => s + e.durationMs, 0) / executions.length)
    : 0

  return (
    <div className="space-y-3">
      {/* Mini stats */}
      <div className="grid grid-cols-3 gap-2 mb-2">
        <div className="bg-dark-700 rounded p-2 text-center">
          <p className="text-green-400 font-bold text-lg">{successCount}</p>
          <p className="text-slate-500 text-xs">Success</p>
        </div>
        <div className="bg-dark-700 rounded p-2 text-center">
          <p className="text-red-400 font-bold text-lg">{failedCount}</p>
          <p className="text-slate-500 text-xs">Failed</p>
        </div>
        <div className="bg-dark-700 rounded p-2 text-center">
          <p className="text-blue-400 font-bold text-lg">{duration(avgDuration)}</p>
          <p className="text-slate-500 text-xs">Avg</p>
        </div>
      </div>

      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-slate-300">Execution History</h3>
        <button
          onClick={() => load(1)}
          className="text-slate-500 hover:text-white transition-colors"
        >
          <RefreshCw size={13} className={loading ? 'animate-spin' : ''} />
        </button>
      </div>

      {/* List */}
      {executions.length === 0 && !loading && (
        <div className="text-center py-8">
          <Clock size={28} className="mx-auto mb-2 text-slate-600" />
          <p className="text-slate-500 text-sm">No executions yet</p>
        </div>
      )}

      <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
        {executions.map(exec => (
          <div key={exec.id}
            className={`rounded-lg p-3 border transition-all
              ${exec.status === 'Running' || exec.status === 'Retrying'
                ? 'bg-dark-700 border-blue-800'
                : exec.status === 'Success'
                ? 'bg-dark-700 border-green-900/50'
                : 'bg-dark-700 border-red-900/50'}`}
          >
            {/* Top row */}
            <div className="flex items-center justify-between mb-1.5">
              <div className="flex items-center gap-2">
                {iconMap[exec.status]}
                <StatusBadge status={exec.status} />
                {exec.attemptNo > 1 && (
                  <span className="text-xs text-yellow-500 bg-yellow-900/30 px-1.5 py-0.5 rounded">
                    Attempt #{exec.attemptNo}
                  </span>
                )}
              </div>
              <span className="text-xs text-slate-500 font-mono">
                {duration(exec.durationMs)}
              </span>
            </div>

            {/* HTTP status */}
            {exec.statusCode && (
              <div className="flex items-center gap-1 mb-1">
                <span className={`text-xs font-mono px-1.5 py-0.5 rounded
                  ${exec.statusCode >= 200 && exec.statusCode < 300
                    ? 'bg-green-900/40 text-green-400'
                    : 'bg-red-900/40 text-red-400'}`}>
                  HTTP {exec.statusCode}
                </span>
              </div>
            )}

            {/* Error */}
            {exec.errorMessage && (
              <p className="text-xs text-red-400 font-mono bg-red-950/30 rounded px-2 py-1 mt-1 break-all">
                {exec.errorMessage}
              </p>
            )}

            {/* Time */}
            <div className="flex items-center justify-between mt-1.5">
              <span className="text-xs text-slate-600 flex items-center gap-1">
                <Clock size={10} />
                {new Date(exec.startedAt).toLocaleString()}
              </span>
              <span className="text-xs text-slate-600">
                {timeAgo(exec.startedAt)}
              </span>
            </div>
          </div>
        ))}
      </div>

      {/* Pagination */}
      {executions.length >= 20 && (
        <div className="flex gap-2 pt-1">
          <button
            disabled={page === 1}
            onClick={() => load(page - 1)}
            className="flex-1 py-1.5 text-xs bg-dark-700 hover:bg-dark-600
                       disabled:opacity-30 rounded transition-colors text-slate-300"
          >
            ← Prev
          </button>
          <span className="flex items-center px-3 text-xs text-slate-500">
            Page {page}
          </span>
          <button
            onClick={() => load(page + 1)}
            className="flex-1 py-1.5 text-xs bg-dark-700 hover:bg-dark-600
                       rounded transition-colors text-slate-300"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  )
}