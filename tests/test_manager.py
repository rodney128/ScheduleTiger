"""Tests for the ScheduleManager."""

import os
import pytest
from datetime import datetime

from schedule_tiger.event import Event
from schedule_tiger.manager import ScheduleManager


@pytest.fixture
def db_path(tmp_path):
    return str(tmp_path / "test_schedule.json")


@pytest.fixture
def manager(db_path):
    return ScheduleManager(db_path=db_path)


def make_event(title="Meeting", start_h=9, end_h=10, date=(2024, 6, 1)):
    return Event(
        title=title,
        start=datetime(*date, start_h, 0),
        end=datetime(*date, end_h, 0),
    )


class TestAddAndGet:
    def test_add_returns_event(self, manager):
        event = make_event()
        result = manager.add(event)
        assert result is event

    def test_get_existing(self, manager):
        event = manager.add(make_event())
        assert manager.get(event.id) is event

    def test_get_missing_returns_none(self, manager):
        assert manager.get("no-such-id") is None

    def test_persistence(self, db_path):
        m1 = ScheduleManager(db_path=db_path)
        event = m1.add(make_event(title="Persisted"))
        m2 = ScheduleManager(db_path=db_path)
        loaded = m2.get(event.id)
        assert loaded is not None
        assert loaded.title == "Persisted"


class TestListAll:
    def test_sorted_by_start(self, manager):
        manager.add(make_event(title="B", start_h=10, end_h=11))
        manager.add(make_event(title="A", start_h=9, end_h=10))
        titles = [e.title for e in manager.list_all()]
        assert titles == ["A", "B"]

    def test_empty_returns_empty_list(self, manager):
        assert manager.list_all() == []


class TestListOn:
    def test_filters_by_date(self, manager):
        manager.add(make_event(title="Day1", date=(2024, 6, 1)))
        manager.add(make_event(title="Day2", date=(2024, 6, 2)))
        results = manager.list_on(datetime(2024, 6, 1))
        assert len(results) == 1
        assert results[0].title == "Day1"

    def test_no_events_on_date(self, manager):
        manager.add(make_event(date=(2024, 6, 1)))
        assert manager.list_on(datetime(2024, 6, 5)) == []


class TestUpdate:
    def test_update_title(self, manager):
        event = manager.add(make_event(title="Old"))
        updated = manager.update(event.id, title="New")
        assert updated.title == "New"

    def test_update_description(self, manager):
        event = manager.add(make_event())
        updated = manager.update(event.id, description="Updated desc")
        assert updated.description == "Updated desc"

    def test_update_missing_returns_none(self, manager):
        assert manager.update("no-such-id", title="X") is None

    def test_update_invalid_times_raises(self, manager):
        event = manager.add(make_event())
        with pytest.raises(ValueError):
            manager.update(
                event.id,
                start=datetime(2024, 6, 1, 10, 0),
                end=datetime(2024, 6, 1, 9, 0),
            )

    def test_update_persists(self, db_path):
        m1 = ScheduleManager(db_path=db_path)
        event = m1.add(make_event(title="Before"))
        m1.update(event.id, title="After")

        m2 = ScheduleManager(db_path=db_path)
        assert m2.get(event.id).title == "After"


class TestDelete:
    def test_delete_existing(self, manager):
        event = manager.add(make_event())
        assert manager.delete(event.id) is True
        assert manager.get(event.id) is None

    def test_delete_missing_returns_false(self, manager):
        assert manager.delete("no-such-id") is False

    def test_delete_persists(self, db_path):
        m1 = ScheduleManager(db_path=db_path)
        event = m1.add(make_event())
        m1.delete(event.id)

        m2 = ScheduleManager(db_path=db_path)
        assert m2.get(event.id) is None


class TestSearch:
    def test_search_by_title(self, manager):
        manager.add(make_event(title="Budget Review"))
        manager.add(make_event(title="Team Standup"))
        results = manager.search("budget")
        assert len(results) == 1
        assert results[0].title == "Budget Review"

    def test_search_by_description(self, manager):
        event = Event(
            title="Meeting",
            start=datetime(2024, 6, 1, 9, 0),
            end=datetime(2024, 6, 1, 10, 0),
            description="discuss quarterly targets",
        )
        manager.add(event)
        results = manager.search("quarterly")
        assert len(results) == 1

    def test_search_case_insensitive(self, manager):
        manager.add(make_event(title="All Hands"))
        assert len(manager.search("all hands")) == 1
        assert len(manager.search("ALL HANDS")) == 1

    def test_search_no_results(self, manager):
        manager.add(make_event(title="Meeting"))
        assert manager.search("nonexistent") == []


class TestFindOverlapping:
    def _make(self, manager, title, start_h, end_h):
        event = Event(
            title=title,
            start=datetime(2024, 6, 1, start_h, 0),
            end=datetime(2024, 6, 1, end_h, 0),
        )
        return manager.add(event)

    def test_no_overlaps(self, manager):
        self._make(manager, "A", 9, 10)
        self._make(manager, "B", 10, 11)
        assert manager.find_overlapping() == []

    def test_one_overlap(self, manager):
        self._make(manager, "A", 9, 11)
        self._make(manager, "B", 10, 12)
        pairs = manager.find_overlapping()
        assert len(pairs) == 1
