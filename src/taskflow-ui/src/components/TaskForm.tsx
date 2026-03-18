import { useState } from 'react'
import { taskApi } from '../api/taskApi'

interface Props {
  onCreated: () => void
  onCancel : () => void
}

export function TaskForm({ onCreated, onCancel }: Props) {
  const [form, setForm] = useState({
    name             : '',
    description      : '',
    scheduleType     : 'Interval',
    cronExpression   : '',
    intervalMinutes  : 10,
    webhookUrl       : '',
    httpMethod       : 'POST',
    webhookBody      : '',
    retryCount       : 3,
    retryDelaySeconds: 30,
    timeoutSeconds   : 60,
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const payload = {
      name             : form.name,
      description      : form.description,
      scheduleType     : form.scheduleType,
      cronExpression   : form.cronExpression,
      intervalMinutes  : form.intervalMinutes,
      webhookUrl       : form.webhookUrl,
      httpMethod       : form.httpMethod,
      webhookBody      : form.webhookBody,
      retryCount       : form.retryCount,
      retryDelaySeconds: form.retryDelaySeconds,
      timeoutSeconds   : form.timeoutSeconds,
    }

    await taskApi.create(payload as any)
    onCreated()
  }

  const field = (label: string, key: keyof typeof form, type = 'text') => (
    <div>
      <label className="block text-xs text-slate-400 mb-1">{label}</label>
      <input
        type={type}
        value={form[key] as string}
        onChange={e => setForm(f => ({ ...f, [key]: type === 'number' ? +e.target.value : e.target.value }))}
        className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2
                   text-white text-sm focus:outline-none focus:border-accent-500"
      />
    </div>
  )

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {field('Task Name', 'name')}
      {field('Description', 'description')}

      <div>
        <label className="block text-xs text-slate-400 mb-1">Schedule Type</label>
        <select
          value={form.scheduleType}
          onChange={e => setForm(f => ({ ...f, scheduleType: e.target.value }))}
          className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2
                     text-white text-sm focus:outline-none focus:border-accent-500"
        >
          <option value="Interval">Interval (every X minutes)</option>
          <option value="Cron">Cron Expression</option>
          <option value="Manual">Manual Only</option>
        </select>
      </div>

      {form.scheduleType === 'Cron'     && field('Cron Expression (e.g. 0 8 * * *)', 'cronExpression')}
      {form.scheduleType === 'Interval' && field('Interval (minutes)', 'intervalMinutes', 'number')}

      {field('Webhook URL', 'webhookUrl')}

      <div>
        <label className="block text-xs text-slate-400 mb-1">HTTP Method</label>
        <select
          value={form.httpMethod}
          onChange={e => setForm(f => ({ ...f, httpMethod: e.target.value }))}
          className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2
                     text-white text-sm focus:outline-none focus:border-accent-500"
        >
          {['POST', 'GET', 'PUT', 'PATCH'].map(m => <option key={m}>{m}</option>)}
        </select>
      </div>

      <div>
        <label className="block text-xs text-slate-400 mb-1">Request Body (JSON)</label>
        <textarea
          value={form.webhookBody}
          onChange={e => setForm(f => ({ ...f, webhookBody: e.target.value }))}
          rows={3}
          className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2
                     text-white text-sm font-mono focus:outline-none focus:border-accent-500"
          placeholder='{"key": "value"}'
        />
      </div>

      <div className="grid grid-cols-3 gap-3">
        {field('Retry Count', 'retryCount', 'number')}
        {field('Retry Delay (s)', 'retryDelaySeconds', 'number')}
        {field('Timeout (s)', 'timeoutSeconds', 'number')}
      </div>

      <div className="flex gap-3 pt-2">
        <button
          type="submit"
          className="flex-1 bg-accent-500 hover:bg-blue-700 text-white py-2 rounded
                     text-sm font-medium transition-colors"
        >
          Create Task
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="flex-1 bg-dark-600 hover:bg-dark-700 text-slate-300 py-2 rounded
                     text-sm transition-colors"
        >
          Cancel
        </button>
      </div>
    </form>
  )
}