import { useCallback, useEffect, useState } from 'react'
import { listTickets, type PagedResult, type Ticket, type TicketFilters } from './api'
import { NewTicketForm } from './components/NewTicketForm'
import { TicketDetail } from './components/TicketDetail'
import { TicketQueue } from './components/TicketQueue'

type Panel = { kind: 'empty' } | { kind: 'new' } | { kind: 'ticket'; ticket: Ticket }

export default function App() {
  const [filters, setFilters] = useState<TicketFilters>({ page: 1 })
  const [result, setResult] = useState<PagedResult<Ticket> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [panel, setPanel] = useState<Panel>({ kind: 'empty' })

  const load = useCallback(async (current: TicketFilters, signal?: AbortSignal) => {
    setLoading(true)
    try {
      const data = await listTickets(current)
      if (!signal?.aborted) {
        setResult(data)
        setError(null)
      }
    } catch (e) {
      if (!signal?.aborted) setError((e as Error).message)
    } finally {
      if (!signal?.aborted) setLoading(false)
    }
  }, [])

  // Petit délai pour ne pas appeler l'API à chaque frappe dans la recherche.
  useEffect(() => {
    const controller = new AbortController()
    const timer = window.setTimeout(() => load(filters, controller.signal), 250)
    return () => {
      controller.abort()
      window.clearTimeout(timer)
    }
  }, [filters, load])

  function handleSaved(ticket: Ticket) {
    setPanel({ kind: 'ticket', ticket })
    void load(filters)
  }

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true" />
          <h1>Helpdesk</h1>
        </div>
        <button type="button" className="primary" onClick={() => setPanel({ kind: 'new' })}>
          Nouveau ticket
        </button>
      </header>

      <main className="workspace">
        <TicketQueue
          filters={filters}
          onFiltersChange={setFilters}
          result={result}
          loading={loading}
          error={error}
          selectedId={panel.kind === 'ticket' ? panel.ticket.id : null}
          onSelect={(ticket) => setPanel({ kind: 'ticket', ticket })}
        />

        <aside className="panel" aria-live="polite">
          {panel.kind === 'new' && <NewTicketForm onCreated={handleSaved} onCancel={() => setPanel({ kind: 'empty' })} />}
          {panel.kind === 'ticket' && <TicketDetail key={`${panel.ticket.id}-${panel.ticket.status}`} ticket={panel.ticket} onUpdated={handleSaved} />}
          {panel.kind === 'empty' && (
            <div className="empty-panel">
              <h2>Aucun ticket sélectionné</h2>
              <p>Choisis un ticket dans la file pour voir son détail, ou crée-en un nouveau.</p>
            </div>
          )}
        </aside>
      </main>
    </div>
  )
}
