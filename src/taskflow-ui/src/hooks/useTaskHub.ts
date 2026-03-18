import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'

interface ExecutionResult {
  taskId      : string
  executionId : string
  isSuccess   : boolean
  durationMs  : number
  errorMessage: string | null
  attemptNo   : number
  willRetry   : boolean
}

export function useTaskHub(
  taskId: string | null,
  onResult: (result: ExecutionResult) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5200/hubs/tasks')
      .withAutomaticReconnect()
      .build()

    connection.on('ExecutionCompleted', onResult)

    connection.start().then(() => {
      if (taskId) connection.invoke('JoinTaskGroup', taskId)
    })

    connectionRef.current = connection

    return () => {
      if (taskId) connection.invoke('LeaveTaskGroup', taskId)
      connection.stop()
    }
  }, [taskId])

  return connectionRef.current
}