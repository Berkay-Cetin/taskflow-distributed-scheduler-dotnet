import { useEffect, useState } from 'react'
import {
  AreaChart, Area, BarChart, Bar,
  XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Legend
} from 'recharts'
import { TrendingUp, TrendingDown, Clock, CheckCircle } from 'lucide-react'
import { statsApi } from '../api/taskApi'
import type { Summary, TimelinePoint, DailyPoint, TaskStat } from '../types'

export function StatsDashboard() {
  const [summary,   setSummary]   = useState<Summary | null>(null)
  const [timeline,  setTimeline]  = useState<TimelinePoint[]>([])
  const [daily,     setDaily]     = useState<DailyPoint[]>([])
  const [taskStats, setTaskStats] = useState<TaskStat[]>([])
  const [loading,   setLoading]   = useState(true)

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const [s, t, d, ts] = await Promise.all([
          statsApi.getSummary(),
          statsApi.getTimeline(),
          statsApi.getDaily(),
          statsApi.getTaskStats(),
        ])
        setSummary(s)
        setTimeline(t.map(x => ({ ...x, hour: new Date(x.hour).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) })))
        setDaily(d.map(x => ({ ...x, date: new Date(x.date).toLocaleDateString([], { month: 'short', day: 'numeric' }) })))
        setTaskStats(ts)
      } finally {
        setLoading(false)
      }
    }
    load()
    const interval = setInterval(load, 30000)
    return () => clearInterval(interval)
  }, [])

  if (loading && !summary) return (
    <div className="text-center py-16 text-slate-500">Loading stats...</div>
  )

  return (
    <div className="space-y-6">

      {/* Success Rate + Avg Duration */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            {
              label: 'Success Rate',
              value: `${summary.executions.successRate}%`,
              icon : <CheckCircle size={18} className="text-green-400" />,
              color: summary.executions.successRate >= 90 ? 'text-green-400' : 'text-yellow-400',
            },
            {
              label: 'Total Executions',
              value: summary.executions.total,
              icon : <TrendingUp size={18} className="text-blue-400" />,
              color: 'text-blue-400',
            },
            {
              label: 'Failed',
              value: summary.executions.failed,
              icon : <TrendingDown size={18} className="text-red-400" />,
              color: 'text-red-400',
            },
            {
              label: 'Avg Duration',
              value: `${summary.executions.avgDurationMs}ms`,
              icon : <Clock size={18} className="text-purple-400" />,
              color: 'text-purple-400',
            },
          ].map(s => (
            <div key={s.label} className="bg-dark-800 border border-dark-600 rounded-lg p-4">
              <div className="flex items-center gap-2 mb-2">
                {s.icon}
                <p className="text-slate-400 text-xs">{s.label}</p>
              </div>
              <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            </div>
          ))}
        </div>
      )}

      {/* 24h Timeline */}
      {timeline.length > 0 && (
        <div className="bg-dark-800 border border-dark-600 rounded-lg p-5">
          <h3 className="text-white font-semibold mb-4">Last 24 Hours</h3>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={timeline}>
              <defs>
                <linearGradient id="success" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%"  stopColor="#22c55e" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="failed" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%"  stopColor="#ef4444" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#ef4444" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#1a1a2e" />
              <XAxis dataKey="hour" tick={{ fill: '#64748b', fontSize: 11 }} />
              <YAxis tick={{ fill: '#64748b', fontSize: 11 }} />
              <Tooltip
                contentStyle={{ background: '#12121a', border: '1px solid #1a1a2e', borderRadius: 8 }}
                labelStyle={{ color: '#94a3b8' }}
              />
              <Legend />
              <Area type="monotone" dataKey="success" stroke="#22c55e" fill="url(#success)" strokeWidth={2} />
              <Area type="monotone" dataKey="failed"  stroke="#ef4444" fill="url(#failed)"  strokeWidth={2} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* 7 Day Daily */}
      {daily.length > 0 && (
        <div className="bg-dark-800 border border-dark-600 rounded-lg p-5">
          <h3 className="text-white font-semibold mb-4">Last 7 Days</h3>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={daily}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1a1a2e" />
              <XAxis dataKey="date" tick={{ fill: '#64748b', fontSize: 11 }} />
              <YAxis tick={{ fill: '#64748b', fontSize: 11 }} />
              <Tooltip
                contentStyle={{ background: '#12121a', border: '1px solid #1a1a2e', borderRadius: 8 }}
                labelStyle={{ color: '#94a3b8' }}
              />
              <Legend />
              <Bar dataKey="success" fill="#22c55e" radius={[4, 4, 0, 0]} />
              <Bar dataKey="failed"  fill="#ef4444" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Task Stats Table */}
      {taskStats.length > 0 && (
        <div className="bg-dark-800 border border-dark-600 rounded-lg overflow-hidden">
          <div className="px-5 py-4 border-b border-dark-600">
            <h3 className="text-white font-semibold">Task Performance</h3>
          </div>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-dark-600 text-slate-400 text-xs">
                <th className="text-left px-5 py-3 font-medium">Task</th>
                <th className="text-left px-4 py-3 font-medium">Total</th>
                <th className="text-left px-4 py-3 font-medium">Success</th>
                <th className="text-left px-4 py-3 font-medium">Failed</th>
                <th className="text-left px-4 py-3 font-medium">Success Rate</th>
                <th className="text-left px-4 py-3 font-medium">Avg Duration</th>
                <th className="text-left px-4 py-3 font-medium">Last Status</th>
              </tr>
            </thead>
            <tbody>
              {taskStats.map((stat, i) => {
                const rate = stat.total > 0
                  ? Math.round(stat.success / stat.total * 100)
                  : 0
                return (
                  <tr key={stat.taskId}
                    className={`border-b border-dark-700 hover:bg-dark-700/60 transition-colors
                      ${i % 2 === 0 ? '' : 'bg-dark-900/20'}`}
                  >
                    <td className="px-5 py-3 text-white font-medium">{stat.taskName}</td>
                    <td className="px-4 py-3 text-slate-300">{stat.total}</td>
                    <td className="px-4 py-3 text-green-400">{stat.success}</td>
                    <td className="px-4 py-3 text-red-400">{stat.failed}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <div className="flex-1 bg-dark-600 rounded-full h-1.5 w-20">
                          <div
                            className={`h-1.5 rounded-full transition-all
                              ${rate >= 90 ? 'bg-green-400' : rate >= 70 ? 'bg-yellow-400' : 'bg-red-400'}`}
                            style={{ width: `${rate}%` }}
                          />
                        </div>
                        <span className={`text-xs font-medium
                          ${rate >= 90 ? 'text-green-400' : rate >= 70 ? 'text-yellow-400' : 'text-red-400'}`}>
                          {rate}%
                        </span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-300 font-mono text-xs">
                      {Math.round(stat.avgDurationMs)}ms
                    </td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded font-medium
                        ${stat.lastStatus === 'Success'  ? 'bg-green-900/40 text-green-400' :
                          stat.lastStatus === 'Failed'   ? 'bg-red-900/40 text-red-400'     :
                          stat.lastStatus === 'Running'  ? 'bg-blue-900/40 text-blue-400'   :
                                                           'bg-slate-800 text-slate-400'}`}>
                        {stat.lastStatus}
                      </span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}