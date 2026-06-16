"""Tests for the Event model."""

import pytest
from datetime import datetime

from schedule_tiger.event import Event


def make_event(**kwargs):
    defaults = dict(
        title="Team Meeting",
        start=datetime(2024, 6, 1, 9, 0),
        end=datetime(2024, 6, 1, 10, 0),
        description="Weekly sync",
    )
    defaults.update(kwargs)
    return Event(**defaults)


class TestEventCreation:
    def test_valid_event(self):
        event = make_event()
        assert event.title == "Team Meeting"
        assert event.description == "Weekly sync"
        assert event.id  # auto-generated UUID

    def test_unique_ids(self):
        e1 = make_event()
        e2 = make_event()
        assert e1.id != e2.id

    def test_empty_title_raises(self):
        with pytest.raises(ValueError, match="title"):
            make_event(title="")

    def test_whitespace_title_raises(self):
        with pytest.raises(ValueError, match="title"):
            make_event(title="   ")

    def test_end_before_start_raises(self):
        with pytest.raises(ValueError, match="end time"):
            make_event(
                start=datetime(2024, 6, 1, 10, 0),
                end=datetime(2024, 6, 1, 9, 0),
            )

    def test_end_equal_to_start_raises(self):
        dt = datetime(2024, 6, 1, 9, 0)
        with pytest.raises(ValueError, match="end time"):
            make_event(start=dt, end=dt)


class TestEventDuration:
    def test_duration_sixty_minutes(self):
        event = make_event(
            start=datetime(2024, 6, 1, 9, 0),
            end=datetime(2024, 6, 1, 10, 0),
        )
        assert event.duration_minutes() == 60

    def test_duration_ninety_minutes(self):
        event = make_event(
            start=datetime(2024, 6, 1, 9, 0),
            end=datetime(2024, 6, 1, 10, 30),
        )
        assert event.duration_minutes() == 90


class TestEventSerialization:
    def test_round_trip(self):
        original = make_event()
        restored = Event.from_dict(original.to_dict())
        assert restored.id == original.id
        assert restored.title == original.title
        assert restored.start == original.start
        assert restored.end == original.end
        assert restored.description == original.description


class TestEventOverlap:
    def _make(self, start_h, end_h):
        return make_event(
            start=datetime(2024, 6, 1, start_h, 0),
            end=datetime(2024, 6, 1, end_h, 0),
        )

    def test_overlapping_events(self):
        e1 = self._make(9, 11)
        e2 = self._make(10, 12)
        assert e1.overlaps(e2)
        assert e2.overlaps(e1)

    def test_non_overlapping_events(self):
        e1 = self._make(9, 10)
        e2 = self._make(10, 11)
        assert not e1.overlaps(e2)
        assert not e2.overlaps(e1)

    def test_contained_event_overlaps(self):
        outer = self._make(9, 12)
        inner = self._make(10, 11)
        assert outer.overlaps(inner)
        assert inner.overlaps(outer)
