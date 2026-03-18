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