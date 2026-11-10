import type { PagedResult, Ticket, TicketCategory, TicketFilters, TicketStatus } from '../api'
import { categoryLabels, formatDate, priorityLabels, statusLabels } from '../labels'

interface Props {
  filters: TicketFilters
  onFiltersChange: (filters: TicketFilters) => void
  result: PagedResult<Ticket> | null
  loading: boolean
  error: string | null
  selectedId: number | null
  onSelect: (ticket: Ticket) => void
}

export function TicketQueue({ filters, onFiltersChange, result, loading, error, selectedId, onSelect }: Props) {
  const pageCount = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1

  return (
    <section className="queue" aria-labelledby="queue-title">
      <div className="queue-head">
        <h2 id="queue-title">File de tickets</h2>
        {result && <p className="count">{result.totalCount} tickets</p>}
      </div>

      <div className="filters">
        <label className="field search">
          <span>Rechercher</span>
          <input
            type="search"
            placeholder="VPN, imprimante, mot de passe…"
            value={filters.search ?? ''}
            onChange={(e) => onFiltersChange({ ...filters, search: e.target.value, page: 1 })}
          />
        </label>
        <label className="field">
          <span>Statut</span>
          <select
            value={filters.status ?? ''}
            onChange={(e) => onFiltersChange({ ...filters, status: (e.target.value || undefined) as TicketStatus | undefined, page: 1 })}
          >
            <option value="">Tous</option>
            {Object.entries(statusLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Catégorie</span>
          <select
            value={filters.category ?? ''}
            onChange={(e) => onFiltersChange({ ...filters, category: (e.target.value || undefined) as TicketCategory | undefined, page: 1 })}
          >
            <option value="">Toutes</option>
            {Object.entries(categoryLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
      </div>

      {error && <p className="notice error" role="alert">{error}</p>}

      {!error && result?.items.length === 0 && !loading && (
        <p className="notice">Aucun ticket ne correspond. Élargis les filtres ou crée un ticket.</p>
      )}

      <ul className="ticket-list" aria-busy={loading}>
        {result?.items.map((ticket) => (
          <li key={ticket.id}>
            <button
              type="button"
              className="ticket-row"
              data-priority={ticket.priority ?? 'none'}
              aria-current={ticket.id === selectedId}
              onClick={() => onSelect(ticket)}
            >
              <span className="row-main">
                <span className="row-title">{ticket.title}</span>
                <span className="row-meta">
                  #{ticket.id} · {formatDate(ticket.createdAt)} · {statusLabels[ticket.status]}
                </span>
              </span>
              <span className="row-tags">
                {ticket.category ? (
                  <span className="tag">{categoryLabels[ticket.category]}</span>
                ) : (
                  <span className="tag pending">À classer</span>
                )}
                {ticket.priority && <span className="priority-label">{priorityLabels[ticket.priority]}</span>}
              </span>
            </button>
          </li>
        ))}
      </ul>

      {result && result.totalCount > result.pageSize && (
        <nav className="pager" aria-label="Pagination">
          <button type="button" disabled={filters.page <= 1} onClick={() => onFiltersChange({ ...filters, page: filters.page - 1 })}>
            Page précédente
          </button>
          <span>Page {filters.page} sur {pageCount}</span>
          <button type="button" disabled={filters.page >= pageCount} onClick={() => onFiltersChange({ ...filters, page: filters.page + 1 })}>
            Page suivante
          </button>
        </nav>
      )}
    </section>
  )
}
