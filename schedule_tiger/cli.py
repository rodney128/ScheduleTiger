"""Command-line interface for ScheduleTiger."""

from __future__ import annotations

import argparse
import sys
from datetime import datetime

from .manager import ScheduleManager, DEFAULT_DB_PATH


def parse_datetime(value: str) -> datetime:
    """Parse a datetime string in ``YYYY-MM-DD HH:MM`` format."""
    try:
        return datetime.strptime(value, "%Y-%m-%d %H:%M")
    except ValueError:
        raise argparse.ArgumentTypeError(
            f"Invalid datetime '{value}'. Use format: YYYY-MM-DD HH:MM"
        )


def parse_date(value: str) -> datetime:
    """Parse a date string in ``YYYY-MM-DD`` format."""
    try:
        return datetime.strptime(value, "%Y-%m-%d")
    except ValueError:
        raise argparse.ArgumentTypeError(
            f"Invalid date '{value}'. Use format: YYYY-MM-DD"
        )


def _fmt_event(event) -> str:
    duration = event.duration_minutes()
    lines = [
        f"  ID:          {event.id}",
        f"  Title:       {event.title}",
        f"  Start:       {event.start.strftime('%Y-%m-%d %H:%M')}",
        f"  End:         {event.end.strftime('%Y-%m-%d %H:%M')}",
        f"  Duration:    {duration} min",
    ]
    if event.description:
        lines.append(f"  Description: {event.description}")
    return "\n".join(lines)


def cmd_add(args: argparse.Namespace, manager: ScheduleManager) -> int:
    from .event import Event

    try:
        event = Event(
            title=args.title,
            start=args.start,
            end=args.end,
            description=args.description or "",
        )
    except ValueError as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    manager.add(event)
    print(f"Added event '{event.title}' (ID: {event.id})")
    return 0


def cmd_list(args: argparse.Namespace, manager: ScheduleManager) -> int:
    if args.date:
        events = manager.list_on(args.date)
        print(f"Events on {args.date.strftime('%Y-%m-%d')}:")
    else:
        events = manager.list_all()
        print("All events:")

    if not events:
        print("  (none)")
        return 0

    for event in events:
        print(_fmt_event(event))
        print()
    return 0


def cmd_get(args: argparse.Namespace, manager: ScheduleManager) -> int:
    event = manager.get(args.id)
    if event is None:
        print(f"Error: No event with ID '{args.id}'", file=sys.stderr)
        return 1
    print(_fmt_event(event))
    return 0


def cmd_update(args: argparse.Namespace, manager: ScheduleManager) -> int:
    try:
        event = manager.update(
            event_id=args.id,
            title=args.title,
            start=args.start,
            end=args.end,
            description=args.description,
        )
    except ValueError as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    if event is None:
        print(f"Error: No event with ID '{args.id}'", file=sys.stderr)
        return 1

    print(f"Updated event '{event.title}' (ID: {event.id})")
    return 0


def cmd_delete(args: argparse.Namespace, manager: ScheduleManager) -> int:
    if not manager.delete(args.id):
        print(f"Error: No event with ID '{args.id}'", file=sys.stderr)
        return 1
    print(f"Deleted event with ID '{args.id}'")
    return 0


def cmd_search(args: argparse.Namespace, manager: ScheduleManager) -> int:
    events = manager.search(args.query)
    print(f"Results for '{args.query}':")
    if not events:
        print("  (none)")
        return 0
    for event in events:
        print(_fmt_event(event))
        print()
    return 0


def cmd_overlaps(args: argparse.Namespace, manager: ScheduleManager) -> int:
    pairs = manager.find_overlapping()
    if not pairs:
        print("No overlapping events found.")
        return 0
    print(f"Found {len(pairs)} overlapping pair(s):")
    for e1, e2 in pairs:
        print(f"  - '{e1.title}' overlaps with '{e2.title}'")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="schedule-tiger",
        description="ScheduleTiger – manage your schedule from the command line.",
    )
    parser.add_argument(
        "--db",
        default=DEFAULT_DB_PATH,
        metavar="PATH",
        help="Path to the JSON data file (default: %(default)s)",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    # add
    p_add = sub.add_parser("add", help="Add a new event")
    p_add.add_argument("title", help="Event title")
    p_add.add_argument("start", type=parse_datetime, help="Start time (YYYY-MM-DD HH:MM)")
    p_add.add_argument("end", type=parse_datetime, help="End time (YYYY-MM-DD HH:MM)")
    p_add.add_argument("-d", "--description", default="", help="Optional description")

    # list
    p_list = sub.add_parser("list", help="List events")
    p_list.add_argument("--date", type=parse_date, help="Filter by date (YYYY-MM-DD)")

    # get
    p_get = sub.add_parser("get", help="Show a specific event")
    p_get.add_argument("id", help="Event ID")

    # update
    p_update = sub.add_parser("update", help="Update an existing event")
    p_update.add_argument("id", help="Event ID")
    p_update.add_argument("--title", help="New title")
    p_update.add_argument("--start", type=parse_datetime, help="New start time (YYYY-MM-DD HH:MM)")
    p_update.add_argument("--end", type=parse_datetime, help="New end time (YYYY-MM-DD HH:MM)")
    p_update.add_argument("--description", help="New description")

    # delete
    p_delete = sub.add_parser("delete", help="Delete an event")
    p_delete.add_argument("id", help="Event ID")

    # search
    p_search = sub.add_parser("search", help="Search events by keyword")
    p_search.add_argument("query", help="Search term")

    # overlaps
    sub.add_parser("overlaps", help="List pairs of overlapping events")

    return parser


def main(argv=None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    manager = ScheduleManager(db_path=args.db)

    commands = {
        "add": cmd_add,
        "list": cmd_list,
        "get": cmd_get,
        "update": cmd_update,
        "delete": cmd_delete,
        "search": cmd_search,
        "overlaps": cmd_overlaps,
    }
    return commands[args.command](args, manager)


if __name__ == "__main__":
    sys.exit(main())
