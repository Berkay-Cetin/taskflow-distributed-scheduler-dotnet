import axios from 'axios'
import type { ScheduledTask, TaskExecution, Summary, TimelinePoint, DailyPoint, TaskStat } from '../types'

const api = axios.create({ baseURL: 'http://localhost:5200/api' })

export const taskApi = {
  getAll: () =>
    api.get<ScheduledTask[]>('/tasks').then(r => r.data),

  create: (data: Partial<ScheduledTask>) =>
    api.post<ScheduledTask>('/tasks', data)
      .then(r => r.data)
      .catch(err => {
        console.log('Validation errors:', err.response?.data)
        throw err
      }),

  update: (id: string, data: Partial<ScheduledTask>) =>
    api.put<ScheduledTask>(`/tasks/${id}`, data).then(r => r.data),

  trigger: (id: string) =>
    api.post(`/tasks/${id}/trigger`).then(r => r.data),

  enable: (id: string) =>
    api.patch(`/tasks/${id}/enable`).then(r => r.data),

  disable: (id: string) =>
    api.patch(`/tasks/${id}/disable`).then(r => r.data),

  getExecutions: (id: string, page = 1) =>
    api.get<TaskExecution[]>(`/tasks/${id}/executions?page=${page}`).then(r => r.data),
}

export const statsApi = {
  getSummary  : () => api.get<Summary>('/stats/summary').then(r => r.data),
  getTimeline : (hours = 24) => api.get<TimelinePoint[]>(`/stats/timeline?hours=${hours}`).then(r => r.data),
  getDaily    : () => api.get<DailyPoint[]>('/stats/daily').then(r => r.data),
  getTaskStats: () => api.get<TaskStat[]>('/stats/tasks').then(r => r.data),
}