"""Génère un jeu de tickets helpdesk fictifs et étiquetés (catégorie, priorité).

Sorties :
  data/tickets.json       historique chargé en base au démarrage de l'API (400 tickets)
  data/tickets-test.json  jeu de test jamais chargé, réservé à l'évaluation (100 tickets)

Le générateur est déterministe (graine fixe) pour que les résultats d'évaluation soient reproductibles.
Usage : python scripts/generate_tickets.py [--seed 42] [--total 500] [--test 100]
"""

from __future__ import annotations

import argparse
import json
import random
from datetime import datetime, timedelta, timezone
from pathlib import Path

PRIORITIES = ["Low", "Medium", "High", "Critical"]

APPS = ["Teams", "SAP", "Salesforce", "l'intranet", "Jira", "Excel", "le logiciel de paie", "GitLab", "le CRM"]
SITES = ["Saint-Denis", "Lyon", "Nantes", "Lille", "Bordeaux", "le siège"]
DEVICES = ["mon PC portable", "mon poste fixe", "mon second écran", "ma station d'accueil", "mon casque"]
FLOORS = ["2e étage", "3e étage", "rez-de-chaussée", "bâtiment B"]

# Chaque modèle : (catégorie, priorité de base, titre, description, résolution type)
TEMPLATES: list[tuple[str, str, str, str, str]] = [
    # Réseau / VPN
    ("Network", "High", "VPN impossible à connecter",
     "Depuis ce matin le VPN refuse la connexion depuis chez moi, message « délai dépassé ». Je ne peux pas accéder à {app}.",
     "Profil VPN réinstallé et certificat renouvelé, connexion rétablie."),
    ("Network", "Medium", "Wi-Fi instable au {floor}",
     "Le Wi-Fi coupe toutes les 10 minutes au {floor} du site de {site}, c'est pénible pendant les visios.",
     "Borne Wi-Fi redémarrée et canal modifié, plus de coupure signalée."),
    ("Network", "Critical", "Plus d'accès réseau sur tout le site de {site}",
     "Personne n'a internet ni accès aux applications sur le site de {site} depuis 9h. Toute l'équipe est bloquée.",
     "Switch principal tombé en panne, remplacé par l'équipe réseau."),
    ("Network", "Low", "Lenteur réseau en fin de journée",
     "Le réseau est un peu lent vers 17h quand j'ouvre {app}, rien de bloquant mais je préfère le signaler.",
     "Pic de charge lié aux sauvegardes, planification décalée à 20h."),
    ("Network", "Medium", "Lecteur réseau partagé inaccessible",
     "Le lecteur partagé de mon service n'apparaît plus sur {device}, je n'ai plus accès à mes dossiers.",
     "Mappage du lecteur recréé par script de connexion."),

    # Compte et accès
    ("Account", "High", "Compte bloqué après plusieurs essais",
     "Mon compte est verrouillé après trois mots de passe erronés, je ne peux plus me connecter à {app}.",
     "Compte déverrouillé après vérification d'identité."),
    ("Account", "Medium", "Mot de passe expiré",
     "On me demande de changer mon mot de passe mais le nouveau est refusé sans explication.",
     "Rappel de la politique de mot de passe (12 caractères, historique), changement effectué."),
    ("Account", "Medium", "Demande d'accès à {app}",
     "Je viens d'arriver dans l'équipe et j'ai besoin d'un accès à {app} pour mes missions.",
     "Accès créé après validation du manager."),
    ("Account", "Low", "Changement de nom d'affichage",
     "Mon nom est mal orthographié dans l'annuaire et dans {app}, pouvez-vous le corriger ?",
     "Annuaire corrigé, synchronisation effectuée."),
    ("Account", "High", "Authentification MFA ne fonctionne plus",
     "J'ai changé de téléphone et je ne reçois plus les codes de double authentification, je suis bloqué.",
     "Méthode MFA réinitialisée et nouvel appareil enregistré."),

    # Matériel
    ("Hardware", "Medium", "Écran qui scintille",
     "{device} scintille par moments, surtout le matin, c'est fatigant pour les yeux.",
     "Câble DisplayPort remplacé."),
    ("Hardware", "High", "PC portable ne démarre plus",
     "{device} ne s'allume plus du tout malgré le chargeur branché. J'ai une réunion client cet après-midi.",
     "Batterie défectueuse, poste de prêt fourni puis réparation sous garantie."),
    ("Hardware", "Low", "Demande de souris ergonomique",
     "Serait-il possible d'avoir une souris verticale ? J'ai des douleurs au poignet en fin de journée.",
     "Souris ergonomique commandée et livrée."),
    ("Hardware", "Medium", "Clavier avec des touches qui ne répondent plus",
     "Plusieurs touches de mon clavier ne répondent plus, je dois appuyer plusieurs fois.",
     "Clavier remplacé."),
    ("Hardware", "Low", "Station d'accueil ne charge pas le portable",
     "Quand je branche le portable sur {device} il ne charge pas, mais les écrans fonctionnent.",
     "Firmware de la station mis à jour."),

    # Logiciel
    ("Software", "High", "{app} plante à l'ouverture",
     "{app} se ferme tout seul dès que je l'ouvre, impossible de travailler depuis la mise à jour d'hier.",
     "Cache applicatif vidé et version corrigée déployée."),
    ("Software", "Medium", "Installation d'un logiciel",
     "J'ai besoin d'installer un outil de diagramme pour mon projet, je n'ai pas les droits administrateur.",
     "Logiciel installé depuis le catalogue d'entreprise."),
    ("Software", "Low", "Mise à jour Windows en attente",
     "Une mise à jour Windows me demande de redémarrer depuis plusieurs jours, est-ce que je peux la reporter ?",
     "Mise à jour installée lors d'un redémarrage planifié."),
    ("Software", "Critical", "{app} indisponible pour tout le service",
     "{app} affiche une erreur 500 pour tous les collègues du site de {site}, la clôture mensuelle est bloquée.",
     "Service applicatif redémarré, incident analysé par l'éditeur."),
    ("Software", "Medium", "Licence {app} expirée",
     "{app} m'indique que ma licence a expiré et passe en lecture seule.",
     "Licence réattribuée depuis la console d'administration."),

    # Impression
    ("Printing", "Low", "Imprimante qui ne recto-verso pas",
     "L'imprimante du {floor} n'imprime plus en recto-verso, ce n'est pas urgent.",
     "Pilote réinstallé avec l'option recto-verso activée."),
    ("Printing", "Medium", "Impossible d'ajouter l'imprimante",
     "Je n'arrive pas à ajouter l'imprimante du {floor} sur {device}, elle n'apparaît pas dans la liste.",
     "File d'impression ajoutée via le portail d'impression."),
    ("Printing", "Medium", "Bourrage papier récurrent",
     "L'imprimante du {floor} à {site} fait des bourrages papier presque à chaque impression.",
     "Intervention du prestataire, rouleau d'entraînement changé."),
    ("Printing", "High", "Impression des contrats bloquée",
     "Plus aucune impression ne sort depuis ce matin à {site}, nous devons imprimer des contrats à signer aujourd'hui.",
     "Serveur d'impression relancé, file débloquée."),

    # Messagerie
    ("Messaging", "Medium", "Boîte mail pleine",
     "Je reçois un message indiquant que ma boîte aux lettres est presque pleine, que dois-je faire ?",
     "Archivage en ligne activé, quota augmenté."),
    ("Messaging", "High", "Mails non reçus depuis ce matin",
     "Je ne reçois plus aucun mail externe depuis ce matin, des clients me disent que leurs messages reviennent.",
     "Règle de transfert erronée supprimée, flux rétabli."),
    ("Messaging", "Critical", "Mail suspect reçu par plusieurs collègues",
     "Plusieurs personnes du site de {site} ont reçu un mail demandant de saisir leurs identifiants. Un collègue a cliqué.",
     "Mail supprimé des boîtes, mot de passe du collègue réinitialisé, incident déclaré à la sécurité."),
    ("Messaging", "Low", "Signature de mail à mettre à jour",
     "Je souhaite mettre à jour ma signature avec le nouveau logo, où trouver le modèle ?",
     "Lien vers le modèle de signature envoyé."),
    ("Messaging", "Medium", "Calendrier partagé non visible",
     "Je ne vois plus le calendrier partagé de mon équipe dans Outlook depuis la migration.",
     "Droits du calendrier réappliqués."),
]

OPENERS = ["Bonjour,", "Bonjour à tous,", "Hello,", "Bonjour l'équipe support,", ""]
CLOSERS = ["Merci d'avance.", "Merci.", "Cordialement.", "Merci pour votre aide !", ""]
URGENT_MARKERS = ["C'est urgent.", "Merci de traiter rapidement.", "Je suis complètement bloqué."]


def shift_priority(base: str, rng: random.Random) -> tuple[str, bool]:
    """Introduit du bruit réaliste : la priorité réelle ne suit pas toujours le modèle."""
    index = PRIORITIES.index(base)
    roll = rng.random()
    urgent = False
    if roll < 0.10 and index < len(PRIORITIES) - 1:
        index += 1
        urgent = True
    elif roll < 0.18 and index > 0:
        index -= 1
    return PRIORITIES[index], urgent


def build_ticket(rng: random.Random, created_at: datetime, reference: datetime) -> dict:
    category, base_priority, title, description, resolution = rng.choice(TEMPLATES)
    values = {
        "app": rng.choice(APPS),
        "site": rng.choice(SITES),
        "device": rng.choice(DEVICES),
        "floor": rng.choice(FLOORS),
    }
    priority, urgent = shift_priority(base_priority, rng)

    body = description.format(**values)
    body = body[0].upper() + body[1:]
    parts = [rng.choice(OPENERS), body]
    if urgent:
        parts.append(rng.choice(URGENT_MARKERS))
    parts.append(rng.choice(CLOSERS))

    title_text = title.format(**values)
    title_text = title_text[0].upper() + title_text[1:]

    age_days = (reference - created_at).days
    if age_days > 14:
        status = "Resolved"
    else:
        status = rng.choice(["Open", "InProgress", "Resolved"])

    return {
        "title": title_text,
        "description": "\n".join(p for p in parts if p),
        "category": category,
        "priority": priority,
        "status": status,
        "resolution": resolution if status == "Resolved" else None,
        "createdAt": created_at.isoformat(timespec="seconds"),
    }


def stratified_split(tickets: list[dict], test_size: int, rng: random.Random) -> tuple[list[dict], list[dict]]:
    """Réserve un jeu de test où chaque catégorie pèse autant que dans l'ensemble."""
    by_category: dict[str, list[dict]] = {}
    for ticket in tickets:
        by_category.setdefault(ticket["category"], []).append(ticket)

    test: list[dict] = []
    history: list[dict] = []
    ratio = test_size / len(tickets)
    for items in by_category.values():
        rng.shuffle(items)
        cut = round(len(items) * ratio)
        test.extend(items[:cut])
        history.extend(items[cut:])

    # Ajuste à la taille demandée si les arrondis ont décalé le total.
    while len(test) > test_size:
        history.append(test.pop())
    while len(test) < test_size:
        test.append(history.pop())

    rng.shuffle(test)
    return test, history


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--total", type=int, default=500)
    parser.add_argument("--test", type=int, default=100, help="nombre de tickets réservés à l'évaluation")
    parser.add_argument("--until", default="2026-11-01", help="date du ticket le plus récent possible (AAAA-MM-JJ)")
    args = parser.parse_args()

    if not 0 < args.test < args.total:
        parser.error("--test doit être strictement compris entre 0 et --total")

    rng = random.Random(args.seed)
    reference = datetime.fromisoformat(args.until).replace(tzinfo=timezone.utc)
    tickets = [
        build_ticket(rng, reference - timedelta(hours=rng.randint(1, 24 * 180)), reference)
        for _ in range(args.total)
    ]

    test, history = stratified_split(tickets, args.test, rng)
    history.sort(key=lambda t: t["createdAt"])

    data_dir = Path(__file__).resolve().parent.parent / "data"
    data_dir.mkdir(exist_ok=True)
    for name, items in (("tickets.json", history), ("tickets-test.json", test)):
        path = data_dir / name
        path.write_text(json.dumps(items, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{len(items):>4} tickets -> {path.relative_to(data_dir.parent)}")


if __name__ == "__main__":
    main()
