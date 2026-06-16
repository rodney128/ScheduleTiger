# ScheduleTiger

ScheduleTiger is a lightweight command-line schedule manager written in Python.
It lets you create, view, update, delete, and search calendar events, with automatic
JSON persistence so your schedule survives between sessions.

## Features

- **Add events** with a title, start/end time, and optional description
- **List events** – all events or filtered by date
- **Get a specific event** by ID
- **Update** any field of an existing event
- **Delete** events by ID
- **Search** events by keyword (case-insensitive, searches title and description)
- **Detect overlapping events** across your schedule
- Persistent JSON storage (default: `~/.schedule_tiger.json`)

## Installation

```bash
pip install -e .
```

## Usage

```
schedule-tiger [--db PATH] <command> [options]
```

### Commands

| Command | Description |
|---------|-------------|
| `add <title> <start> <end> [-d description]` | Add a new event |
| `list [--date YYYY-MM-DD]` | List all events (optionally filter by date) |
| `get <id>` | Show a specific event |
| `update <id> [--title] [--start] [--end] [--description]` | Update an event |
| `delete <id>` | Delete an event |
| `search <query>` | Search events by keyword |
| `overlaps` | List pairs of overlapping events |

Dates and times use the format `YYYY-MM-DD HH:MM`.

### Examples

```bash
# Add an event
schedule-tiger add "Team Standup" "2024-06-01 09:00" "2024-06-01 09:30" -d "Daily sync"

# List all events
schedule-tiger list

# List events on a specific date
schedule-tiger list --date 2024-06-01

# Search for events
schedule-tiger search standup

# Update an event title
schedule-tiger update <id> --title "Morning Standup"

# Delete an event
schedule-tiger delete <id>

# Find overlapping events
schedule-tiger overlaps

# Use a custom data file
schedule-tiger --db ~/work/schedule.json list
```

## Running Tests

```bash
pip install pytest
pytest
```
