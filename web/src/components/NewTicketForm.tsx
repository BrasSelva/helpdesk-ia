import { useState } from 'react'
import { ApiError, createTicket, type Ticket } from '../api'

interface Props {
  onCreated: (ticket: Ticket) => void
  onCancel: () => void
}

export function NewTicketForm({ onCreated, onCancel }: Props) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const [error, setError] = useState<string | null>(null)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setSaving(true)
    setErrors({})
    setError(null)
    try {
      onCreated(await createTicket(title, description))
    } catch (e) {
      const apiError = e as ApiError
      setErrors(apiError.fieldErrors ?? {})
      if (!apiError.fieldErrors || Object.keys(apiError.fieldErrors).length === 0) setError(apiError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="detail new-ticket" onSubmit={submit} aria-labelledby="new-title">
      <h2 id="new-title">Nouveau ticket</h2>
      <p className="hint">Décris le problème comme le ferait un collègue. Catégorie et priorité seront déterminées plus tard par le modèle.</p>

      <label className="field">
        <span>Titre</span>
        <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Ex. VPN impossible à connecter" aria-invalid={!!errors.title} />
        {errors.title && <small className="field-error">{errors.title[0]}</small>}
      </label>

      <label className="field">
        <span>Description</span>
        <textarea
          rows={6}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Ce qui se passe, depuis quand, ce qui est bloqué"
          aria-invalid={!!errors.description}
        />
        {errors.description && <small className="field-error">{errors.description[0]}</small>}
      </label>

      {error && <p className="notice error" role="alert">{error}</p>}

      <div className="actions">
        <button type="submit" className="primary" disabled={saving}>{saving ? 'Création…' : 'Créer le ticket'}</button>
        <button type="button" className="ghost" onClick={onCancel}>Annuler</button>
      </div>
    </form>
  )
}
