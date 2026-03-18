import type { TaskStatus, ExecStatus } from '../types'

const colors: Record<string, string> = {
  Idle     : 'bg-slate-700 text-slate-300',
  Running  : 'bg-blue-900 text-blue-300 animate-pulse',
  Success  : 'bg-green-900 text-green-300',
  Failed   : 'bg-red-900 text-red-300',
  Disabled : 'bg-gray-800 text-gray-500',
  Retrying : 'bg-yellow-900 text-yellow-300 animate-pulse',
  Dead     : 'bg-red-950 text-red-400',
}

export function StatusBadge({ status }: { status: TaskStatus | ExecStatus }) {
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-medium ${colors[status] ?? colors.Idle}`}>
      {status}
    </span>
  )
}