import axios from 'axios'
import type { ScheduledTask, TaskExecution, Summary, TimelinePoint, DailyPoint, TaskStat, AuthResponse, AuthUser } from '../types'

const api = axios.create({ baseURL: 'http://localhost:5200/api' })

// Token ekle
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// 401 → refresh token
api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config
    if (err.response?.status === 401 && !original._retry) {
      original._retry = true
      try {
        const refreshToken = localStorage.getItem('refreshToken')
        if (!refreshToken) throw new Error('No refresh token')

        const res = await axios.post(
          'http://localhost:5200/api/auth/refresh',
          { refreshToken }
        )
        localStorage.setItem('accessToken',  res.data.accessToken)
        localStorage.setItem('refreshToken', res.data.refreshToken)
        original.headers.Authorization = `Bearer ${res.data.accessToken}`
        return api(original)
      } catch {
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
        window.location.href = '/'
      }
    }
    return Promise.reject(err)
  }
)

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

export const authApi = {
  login: (username: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { username, password }).then(r => r.data),

  register: (username: string, email: string, password: string, role = 'Viewer') =>
    api.post('/auth/register', { username, email, password, role }).then(r => r.data),

  refresh: (refreshToken: string) =>
    api.post<AuthResponse>('/auth/refresh', { refreshToken }).then(r => r.data),

  me: () =>
    api.get<AuthUser>('/auth/me').then(r => r.data),

  logout: () =>
    api.post('/auth/logout').then(r => r.data),
}