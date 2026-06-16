"""Event model for ScheduleTiger."""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field
from datetime import datetime


@dataclass
class Event:
    """Represents a scheduled event."""

    title: str
    start: datetime
    end: datetime
    description: str = ""
    id: str = field(default_factory=lambda: str(uuid.uuid4()))

    def __post_init__(self) -> None:
        if not self.title or not self.title.strip():
            raise ValueError("Event title cannot be empty.")
        if self.end <= self.start:
            raise ValueError("Event end time must be after start time.")

    def duration_minutes(self) -> int:
        """Return the duration of the event in minutes."""
        return int((self.end - self.start).total_seconds() // 60)

    def to_dict(self) -> dict:
        """Serialise event to a JSON-friendly dictionary."""
        return {
            "id": self.id,
            "title": self.title,
            "start": self.start.isoformat(),
            "end": self.end.isoformat(),
            "description": self.description,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "Event":
        """Deserialise an event from a dictionary."""
        return cls(
            id=data["id"],
            title=data["title"],
            start=datetime.fromisoformat(data["start"]),
            end=datetime.fromisoformat(data["end"]),
            description=data.get("description", ""),
        )

    def overlaps(self, other: "Event") -> bool:
        """Return True if this event overlaps with *other*."""
        return self.start < other.end and self.end > other.start
