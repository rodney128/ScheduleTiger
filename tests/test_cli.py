"""Tests for the CLI."""

import pytest
from datetime import datetime

from schedule_tiger.cli import main


@pytest.fixture
def db(tmp_path):
    return str(tmp_path / "cli_test.json")


def run(*args, db):
    return main(["--db", db] + list(args))


class TestCLIAdd:
    def test_add_success(self, db):
        rc = run("add", "Standup", "2024-06-01 09:00", "2024-06-01 09:30", db=db)
        assert rc == 0

    def test_add_invalid_time_order(self, db, capsys):
        rc = run("add", "Bad", "2024-06-01 10:00", "2024-06-01 09:00", db=db)
        assert rc == 1
        assert "Error" in capsys.readouterr().err


class TestCLIList:
    def test_list_empty(self, db, capsys):
        rc = run("list", db=db)
        assert rc == 0
        assert "none" in capsys.readouterr().out

    def test_list_shows_events(self, db, capsys):
        run("add", "Sprint Planning", "2024-06-01 10:00", "2024-06-01 11:00", db=db)
        rc = run("list", db=db)
        assert rc == 0
        assert "Sprint Planning" in capsys.readouterr().out

    def test_list_date_filter(self, db, capsys):
        run("add", "Day1 Event", "2024-06-01 09:00", "2024-06-01 10:00", db=db)
        run("add", "Day2 Event", "2024-06-02 09:00", "2024-06-02 10:00", db=db)
        capsys.readouterr()  # discard output from add commands
        rc = run("list", "--date", "2024-06-01", db=db)
        assert rc == 0
        out = capsys.readouterr().out
        assert "Day1 Event" in out
        assert "Day2 Event" not in out


class TestCLIGet:
    def test_get_existing(self, db, capsys):
        run("add", "Retro", "2024-06-01 14:00", "2024-06-01 15:00", db=db)
        from schedule_tiger.manager import ScheduleManager
        manager = ScheduleManager(db_path=db)
        event_id = manager.list_all()[0].id
        rc = run("get", event_id, db=db)
        assert rc == 0
        assert "Retro" in capsys.readouterr().out

    def test_get_missing(self, db, capsys):
        rc = run("get", "no-such-id", db=db)
        assert rc == 1
        assert "Error" in capsys.readouterr().err


class TestCLIDelete:
    def test_delete_existing(self, db, capsys):
        run("add", "ToDelete", "2024-06-01 09:00", "2024-06-01 10:00", db=db)
        from schedule_tiger.manager import ScheduleManager
        manager = ScheduleManager(db_path=db)
        event_id = manager.list_all()[0].id
        rc = run("delete", event_id, db=db)
        assert rc == 0
        assert "Deleted" in capsys.readouterr().out

    def test_delete_missing(self, db, capsys):
        rc = run("delete", "no-such-id", db=db)
        assert rc == 1
        assert "Error" in capsys.readouterr().err


class TestCLISearch:
    def test_search_found(self, db, capsys):
        run("add", "Board Meeting", "2024-06-01 09:00", "2024-06-01 10:00", db=db)
        rc = run("search", "board", db=db)
        assert rc == 0
        assert "Board Meeting" in capsys.readouterr().out

    def test_search_not_found(self, db, capsys):
        rc = run("search", "nonexistent", db=db)
        assert rc == 0
        assert "none" in capsys.readouterr().out


class TestCLIOverlaps:
    def test_no_overlaps(self, db, capsys):
        run("add", "A", "2024-06-01 09:00", "2024-06-01 10:00", db=db)
        run("add", "B", "2024-06-01 10:00", "2024-06-01 11:00", db=db)
        rc = run("overlaps", db=db)
        assert rc == 0
        assert "No overlapping" in capsys.readouterr().out

    def test_with_overlap(self, db, capsys):
        run("add", "A", "2024-06-01 09:00", "2024-06-01 11:00", db=db)
        run("add", "B", "2024-06-01 10:00", "2024-06-01 12:00", db=db)
        rc = run("overlaps", db=db)
        assert rc == 0
        out = capsys.readouterr().out
        assert "A" in out and "B" in out
