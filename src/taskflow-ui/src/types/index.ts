export type ScheduleType = 'Cron' | 'Interval' | 'Manual'
export type TaskStatus   = 'Idle' | 'Running' | 'Success' | 'Failed' | 'Disabled'
export type ExecStatus   = 'Running' | 'Success' | 'Failed' | 'Retrying' | 'Dead'

export interface ScheduledTask {
  id               : string
  name             : string
  description?     : string
  scheduleType     : ScheduleType
  cronExpression?  : string
  intervalMinutes? : number
  webhookUrl       : string
  httpMethod       : string
  retryCount       : number
  retryDelaySeconds: number
  timeoutSeconds   : number
  isEnabled        : boolean
  lastRunAt?       : string
  nextRunAt?       : string
  lastStatus       : TaskStatus
  createdAt        : string
}

export interface TaskExecution {
  id          : string
  taskId      : string
  taskName    : string
  status      : ExecStatus
  attemptNo   : number
  errorMessage: string | null
  statusCode  : number | null
  durationMs  : number
  startedAt   : string
  finishedAt  : string | null
}

export interface Summary {
  tasks: {
    total   : number
    enabled : number
    disabled: number
    running : number
    failed  : number
  }
  executions: {
    total        : number
    success      : number
    failed       : number
    successRate  : number
    avgDurationMs: number
  }
}

export interface TimelinePoint {
  hour   : string
  total  : number
  success: number
  failed : number
}

export interface DailyPoint {
  date          : string
  total         : number
  success       : number
  failed        : number
  avgDurationMs : number
}

export interface TaskStat {
  taskId       : string
  taskName     : string
  total        : number
  success      : number
  failed       : number
  avgDurationMs: number
  lastRunAt    : string | null
  lastStatus   : string
}