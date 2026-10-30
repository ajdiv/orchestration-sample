"""
enrich-fx-rates: a scripted component.

Reads the parsed transactions from the file at INPUT_PATH, adds a USD amount to each one,
and prints the result. The platform stores what it prints and tells the workflow it's done.
"""
import json
import os
import sys

# Stand-in for a rates service.
RATES = {"USD": 1.0, "EUR": 1.08, "GBP": 1.27, "JPY": 0.0066}

with open(os.environ["INPUT_PATH"]) as f:
    rows = json.load(f)

for row in rows:
    row["AmountUsd"] = round(row["Amount"] * RATES.get(row["Currency"], 1.0), 2)

print(json.dumps(rows))
print(f"enriched {len(rows)} rows", file=sys.stderr)
