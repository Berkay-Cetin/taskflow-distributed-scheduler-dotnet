import { Play, Pause, PlayCircle, Clock, Globe } from 'lucide-react'
import type { ScheduledTask } from '../types'
import { StatusBadge } from './StatusBadge'
import { taskApi } from '../api/taskApi'

interface Props {
  task    : ScheduledTask
  onSelect: (task: ScheduledTask) => void
  onRefresh: () => void
}

export function TaskCard({ task, onSelect, onRefresh }: Props) {
  const handleToggle = async (e: React.MouseEvent) => {
    e.stopPropagation()
    task.isEnabled ? await taskApi.disable(task.id) : await taskApi.enable(task.id)
    onRefresh()
  }

  const handleTrigger = async (e: React.MouseEvent) => {
    e.stopPropagation()
    await taskApi.trigger(task.id)
    onRefresh()
  }

  return (
    <div
      onClick={() => onSelect(task)}
      className="bg-dark-800 border border-dark-600 rounded-lg p-4 cursor-pointer
                 hover:border-accent-500 transition-all duration-200 hover:shadow-lg
                 hover:shadow-accent-500/10"
    >
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="font-semibold text-white">{task.name}</h3>
          {task.description && (
            <p className="text-slate-400 text-sm mt-0.5">{task.description}</p>
          )}
        </div>
        <StatusBadge status={task.lastStatus} />
      </div>

      <div className="flex items-center gap-2 text-slate-400 text-xs mb-3">
        <Globe size={12} />
        <span className="truncate">{task.webhookUrl}</span>
      </div>

      <div className="flex items-center gap-2 text-slate-400 text-xs mb-4">
        <Clock size={12} />
        <span>
          {task.scheduleType === 'Cron'     && task.cronExpression}
          {task.scheduleType === 'Interval' && `Every ${task.intervalMinutes}m`}
          {task.scheduleType === 'Manual'   && 'Manual only'}
        </span>
        {task.nextRunAt && (
          <span className="ml-auto text-slate-500">
            Next: {new Date(task.nextRunAt).toLocaleTimeString()}
          </span>
        )}
      </div>

      <div className="flex gap-2">
        <button
          onClick={handleTrigger}
          className="flex items-center gap-1 px-3 py-1.5 bg-accent-500 hover:bg-blue-700
                     text-white text-xs rounded transition-colors"
        >
          <PlayCircle size={12} /> Run
        </button>
        <button
          onClick={handleToggle}
          className={`flex items-center gap-1 px-3 py-1.5 text-xs rounded transition-colors
            ${task.isEnabled
              ? 'bg-dark-600 hover:bg-red-900 text-slate-300'
              : 'bg-dark-600 hover:bg-green-900 text-slate-300'}`}
        >
          {task.isEnabled ? <><Pause size={12} /> Disable</> : <><Play size={12} /> Enable</>}
        </button>
      </div>
    </div>
  )
}