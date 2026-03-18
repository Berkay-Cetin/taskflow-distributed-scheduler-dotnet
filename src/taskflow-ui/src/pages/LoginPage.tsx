import { useState } from 'react'
import { Activity } from 'lucide-react'
import { useAuth } from '../context/AuthContext'

export function LoginPage() {
  const { login }   = useAuth()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error,    setError]    = useState('')
  const [loading,  setLoading]  = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(username, password)
    } catch {
      setError('Invalid username or password')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-dark-900 flex items-center justify-center">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="flex items-center justify-center gap-3 mb-2">
            <Activity className="text-blue-400" size={32} />
            <h1 className="text-3xl font-bold text-white">TaskFlow</h1>
          </div>
          <p className="text-slate-400 text-sm">Distributed Task Scheduler</p>
        </div>

        {/* Form */}
        <div className="bg-dark-800 border border-dark-600 rounded-xl p-8">
          <h2 className="text-white font-semibold text-lg mb-6">Sign In</h2>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs text-slate-400 mb-1">Username</label>
              <input
                type="text"
                value={username}
                onChange={e => setUsername(e.target.value)}
                className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2.5
                           text-white text-sm focus:outline-none focus:border-blue-500"
                placeholder="admin"
                autoFocus
              />
            </div>

            <div>
              <label className="block text-xs text-slate-400 mb-1">Password</label>
              <input
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="w-full bg-dark-700 border border-dark-600 rounded px-3 py-2.5
                           text-white text-sm focus:outline-none focus:border-blue-500"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <p className="text-red-400 text-xs bg-red-950/30 border border-red-900/50
                            rounded px-3 py-2">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 hover:bg-blue-700 disabled:opacity-50
                         text-white py-2.5 rounded text-sm font-medium transition-colors"
            >
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}