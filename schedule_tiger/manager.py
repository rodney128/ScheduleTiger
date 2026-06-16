"""Schedule manager – CRUD operations and persistent JSON storage."""

from __future__ import annotations

import json
import os
from datetime import datetime
from typing import List, Optional

from .event import Event

DEFAULT_DB_PATH = os.path.join(os.path.expanduser("~"), ".schedule_tiger.json")


class ScheduleManager:
    """Manages a collection of :class:`Event` objects with JSON persistence."""

    def __init__(self, db_path: str = DEFAULT_DB_PATH) -> None:
        self.db_path = db_path
        self._events: List[Event] = []
        self._load()

    # ------------------------------------------------------------------
    # Persistence
    # ------------------------------------------------------------------

    def _load(self) -> None:
        if not os.path.exists(self.db_path):
            return
        with open(self.db_path, "r", encoding="utf-8") as fh:
            data = json.load(fh)
        self._events = [Event.from_dict(item) for item in data]

    def _save(self) -> None:
        with open(self.db_path, "w", encoding="utf-8") as fh:
            json.dump([e.to_dict() for e in self._events], fh, indent=2)

    # ------------------------------------------------------------------
    # CRUD
    # ------------------------------------------------------------------

    def add(self, event: Event) -> Event:
        """Add *event* to the schedule and persist."""
        self._events.append(event)
        self._save()
        return event

    def get(self, event_id: str) -> Optional[Event]:
        """Return the event with *event_id*, or ``None`` if not found."""
        for event in self._events:
            if event.id == event_id:
                return event
        return None

    def update(
        self,
        event_id: str,
        title: Optional[str] = None,
        start: Optional[datetime] = None,
        end: Optional[datetime] = None,
        description: Optional[str] = None,
    ) -> Optional[Event]:
        """Update an existing event's fields and persist.

        Returns the updated event, or ``None`` if *event_id* was not found.
        """
        event = self.get(event_id)
        if event is None:
            return None

        new_title = title if title is not None else event.title
        new_start = start if start is not None else event.start
        new_end = end if end is not None else event.end
        new_description = description if description is not None else event.description

        # Validate before mutating
        if new_end <= new_start:
            raise ValueError("Event end time must be after start time.")
        if not new_title.strip():
            raise ValueError("Event title cannot be empty.")

        event.title = new_title
        event.start = new_start
        event.end = new_end
        event.description = new_description
        self._save()
        return event

    def delete(self, event_id: str) -> bool:
        """Delete the event with *event_id*.  Returns ``True`` on success."""
        before = len(self._events)
        self._events = [e for e in self._events if e.id != event_id]
        changed = len(self._events) < before
        if changed:
            self._save()
        return changed

    # ------------------------------------------------------------------
    # Queries
    # ------------------------------------------------------------------

    def list_all(self) -> List[Event]:
        """Return all events sorted by start time."""
        return sorted(self._events, key=lambda e: e.start)

    def list_on(self, date: datetime) -> List[Event]:
        """Return events whose start date matches *date* (year/month/day)."""
        return [
            e
            for e in self.list_all()
            if e.start.date() == date.date()
        ]

    def search(self, query: str) -> List[Event]:
        """Return events whose title or description contain *query* (case-insensitive)."""
        q = query.lower()
        return [
            e
            for e in self.list_all()
            if q in e.title.lower() or q in e.description.lower()
        ]

    def find_overlapping(self) -> List[tuple]:
        """Return pairs of events that overlap in time."""
        events = self.list_all()
        overlaps = []
        for i, e1 in enumerate(events):
            for e2 in events[i + 1:]:
                if e1.overlaps(e2):
                    overlaps.append((e1, e2))
        return overlaps
