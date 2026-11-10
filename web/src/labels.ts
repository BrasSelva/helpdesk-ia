import type { TicketCategory, TicketPriority, TicketStatus } from './api'

export const categoryLabels: Record<TicketCategory, string> = {
  Network: 'Réseau et VPN',
  Account: 'Compte et accès',
  Hardware: 'Matériel',
  Software: 'Logiciel',
  Printing: 'Impression',
  Messaging: 'Messagerie',
}

export const priorityLabels: Record<TicketPriority, string> = {
  Low: 'Basse',
  Medium: 'Moyenne',
  High: 'Haute',
  Critical: 'Critique',
}

export const statusLabels: Record<TicketStatus, string> = {
  Open: 'Ouvert',
  InProgress: 'En cours',
  Resolved: 'Résolu',
}

const dateFormat = new Intl.DateTimeFormat('fr-FR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })

export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso))
}
