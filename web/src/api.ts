export type TicketCategory = 'Network' | 'Account' | 'Hardware' | 'Software' | 'Printing' | 'Messaging'
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical'
export type TicketStatus = 'Open' | 'InProgress' | 'Resolved'

export interface Ticket {
  id: number
  title: string
  description: string
  category: TicketCategory | null
  priority: TicketPriority | null
  status: TicketStatus
  resolution: string | null
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface TicketFilters {
  status?: TicketStatus
  category?: TicketCategory
  search?: string
  page: number
}

/** Erreur renvoyée par l'API, avec les messages de validation par champ quand il y en a. */
export class ApiError extends Error {
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.fieldErrors = fieldErrors
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...init?.headers },
    })
  } catch {
    throw new ApiError("L'API ne répond pas. Vérifie qu'elle tourne sur le port 5080.")
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(problem?.title ?? `Erreur ${response.status}`, problem?.errors ?? {})
  }

  return response.json() as Promise<T>
}

export function listTickets(filters: TicketFilters): Promise<PagedResult<Ticket>> {
  const params = new URLSearchParams({ page: String(filters.page), pageSize: '20' })
  if (filters.status) params.set('status', filters.status)
  if (filters.category) params.set('category', filters.category)
  if (filters.search?.trim()) params.set('search', filters.search.trim())
  return request(`/api/tickets?${params}`)
}

export function createTicket(title: string, description: string): Promise<Ticket> {
  return request('/api/tickets', { method: 'POST', body: JSON.stringify({ title, description }) })
}

export function classifyTicket(id: number): Promise<Ticket> {
    return request(`/api/tickets/${id}/classify`, {method: 'POST'})
} 

export function updateStatus(id: number, status: TicketStatus, resolution: string): Promise<Ticket> {
  return request(`/api/tickets/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status, resolution: resolution.trim() || null }),
  })
}
