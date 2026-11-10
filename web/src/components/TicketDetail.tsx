import { useState } from 'react'
import { ApiError, updateStatus, type Ticket, type TicketStatus } from '../api'
import { categoryLabels, formatDate, priorityLabels, statusLabels } from '../labels'

interface Props {
  ticket: Ticket
  onUpdated: (ticket: Ticket) => void
}

export function TicketDetail({ ticket, onUpdated }: Props) {
  const [status, setStatus] = useState<TicketStatus>(ticket.status)
  const [resolution, setResolution] = useState(ticket.resolution ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const changed = status !== ticket.status || resolution.trim() !== (ticket.resolution ?? '')

  async function save(event: React.FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      onUpdated(await updateStatus(ticket.id, status, resolution))
    } catch (e) {
      const apiError = e as ApiError
      setError(apiError.fieldErrors?.resolution?.[0] ?? apiError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <article className="detail" data-priority={ticket.priority ?? 'none'} aria-labelledby="detail-title">
      <p className="detail-meta">Ticket #{ticket.id}, ouvert le {formatDate(ticket.createdAt)}</p>
      <h2 id="detail-title">{ticket.title}</h2>

      <dl className="facts">
        <div>
          <dt>Catégorie</dt>
          <dd>{ticket.category ? categoryLabels[ticket.category] : 'Pas encore classé'}</dd>
        </div>
        <div>
          <dt>Priorité</dt>
          <dd>{ticket.priority ? priorityLabels[ticket.priority] : 'Pas encore évaluée'}</dd>
        </div>
        <div>
          <dt>Statut</dt>
          <dd>{statusLabels[ticket.status]}</dd>
        </div>
      </dl>

      <p className="description">{ticket.description}</p>

      {!ticket.category && (
        <p className="notice">Le classement automatique arrive en semaine 2 : ce ticket sera trié par le modèle.</p>
      )}

      <form className="status-form" onSubmit={save}>
        <label className="field">
          <span>Statut</span>
          <select value={status} onChange={(e) => setStatus(e.target.value as TicketStatus)}>
            {Object.entries(statusLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Résolution {status === 'Resolved' ? '(obligatoire)' : '(facultative)'}</span>
          <textarea
            rows={3}
            value={resolution}
            onChange={(e) => setResolution(e.target.value)}
            placeholder="Ce qui a été fait pour résoudre le problème"
          />
        </label>
        {error && <p className="notice error" role="alert">{error}</p>}
        <button type="submit" className="primary" disabled={!changed || saving}>
          {saving ? 'Enregistrement…' : 'Enregistrer le statut'}
        </button>
      </form>
    </article>
  )
}
