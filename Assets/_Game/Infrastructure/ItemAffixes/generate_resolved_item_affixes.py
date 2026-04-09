"""
Resolves ItemAffixPool + AffixTemplates + AffixExceptions into a flat
ResolvedItemAffixPool CSV (one row per affix tier).

Default: emit ALL tiers (1..tierCount) for every mod — runtime master catalog.
Tier windows from DropQualityProgression are applied in-game, not here.

Usage (run from this folder, or pass --data-dir):
  python generate_resolved_item_affixes.py
  python generate_resolved_item_affixes.py --data-dir .
  python generate_resolved_item_affixes.py --progress-band 10
    (optional slice only — for previews)

Expected CSV files in --data-dir:
  - Новая таблица - ItemAffixPool (6).csv  (or --item-pool path)
  - AffixTemplates.csv
  - AffixExceptions.csv
  - DropQualityProgression.csv — only if --progress-band is set

Tier numbering (display): T1 = strongest / best roll, Tn = weakest (e.g. T8 worst).
Internal formula still uses strength 1..n where low=weak, high=strong; we map
display_t -> internal_t = n - display_t + 1.

Note: tierCount per template comes from AffixTemplates.csv (e.g. premium_offense
has tierCount=6, so labels are T1..T6 — not a script bug).

If you use --progress-band, allowedTierMin/Max in DropQualityProgression must use
the SAME convention (T1 best, high number worse). Update that CSV if it was
authored for the old reversed scheme.
"""

from __future__ import annotations

import argparse
import csv
import sys
from pathlib import Path


def read_csv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def apply_round(value: float, round_mode: str) -> float:
    if round_mode == "1dp":
        return round(value, 1)
    if round_mode == "2dp":
        return round(value, 2)
    return float(int(round(value)))


def tier_value(template: dict[str, str], t: int, n: int) -> tuple[float, float]:
    """t = internal strength step: 1 = weakest, n = strongest (formula input)."""
    min_base = float(template["minBase"])
    max_base = float(template["maxBase"])
    min_cap = float(template["minCap"])
    max_cap = float(template["maxCap"])

    round_mode = template.get("roundMode", "int")

    if n <= 1:
        return apply_round(min_base, round_mode), apply_round(max_base, round_mode)

    min_v = min_cap * ((min_base / min_cap) ** ((n - t) / (n - 1)))
    max_v = max_cap * ((max_base / max_cap) ** ((n - t) / (n - 1)))

    lo_min, hi_min = (min_base, min_cap) if min_base <= min_cap else (min_cap, min_base)
    lo_max, hi_max = (max_base, max_cap) if max_base <= max_cap else (max_cap, max_base)
    min_v = min(max(min_v, lo_min), hi_min)
    max_v = min(max(max_v, lo_max), hi_max)

    if min_v > max_v:
        min_v, max_v = max_v, min_v

    return apply_round(min_v, round_mode), apply_round(max_v, round_mode)


def tier_weight(base_weight: int, curve_id: str, t: int) -> int:
    """t = internal strength step (1 = weakest, n = strongest), same as tier_value."""
    curve_drops = {
        "core_soft": 0.08,
        "broad_soft": 0.06,
        "premium_soft": 0.10,
        "special_soft": 0.12,
    }
    drop = curve_drops.get(curve_id, 0.08)
    return max(1, int(round(base_weight * (1.0 - drop * (t - 1)))))


def parse_override_weights(text: str) -> dict[int, float]:
    result: dict[int, float] = {}
    if not text:
        return result
    for pair in text.split(","):
        idx, factor = pair.split(":")
        result[int(idx.strip())] = float(factor.strip())
    return result


def display_tier_to_internal_strength(display_t: int, n: int) -> int:
    """Map display tier (T1 best … Tn worst) to internal strength index (1 weak … n strong)."""
    return n - display_t + 1


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Resolve ItemAffixPool into tier rows (min/max/weight). "
        "Default: all tiers; use --progress-band only for sliced previews."
    )
    parser.add_argument(
        "--data-dir",
        type=Path,
        default=Path(__file__).resolve().parent,
        help="Folder with CSVs (default: directory of this script).",
    )
    parser.add_argument(
        "--item-pool",
        type=Path,
        default=None,
        help="ItemAffixPool CSV path. Default: <data-dir>/Новая таблица - ItemAffixPool (6).csv",
    )
    parser.add_argument(
        "--progress-band",
        type=int,
        default=None,
        help="If set, only tiers in DropQualityProgression window for this band.",
    )
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        default=None,
        help="Output CSV (default: <data-dir>/ResolvedItemAffixPool.csv)",
    )
    args = parser.parse_args()

    data_dir: Path = args.data_dir
    item_pool = args.item_pool or (
        data_dir / "Новая таблица - ItemAffixPool (6).csv"
    )
    templates_path = data_dir / "AffixTemplates.csv"
    exceptions_path = data_dir / "AffixExceptions.csv"
    progression_path = data_dir / "DropQualityProgression.csv"
    out_path = args.output or (data_dir / "ResolvedItemAffixPool.csv")

    if not item_pool.is_file():
        print(f"ERROR: Item pool not found: {item_pool}", file=sys.stderr)
        sys.exit(1)
    if not templates_path.is_file():
        print(f"ERROR: AffixTemplates.csv not found: {templates_path}", file=sys.stderr)
        sys.exit(1)
    if not exceptions_path.is_file():
        print(f"ERROR: AffixExceptions.csv not found: {exceptions_path}", file=sys.stderr)
        sys.exit(1)

    affixes = read_csv(item_pool)
    templates = {row["templateId"]: row for row in read_csv(templates_path)}
    exceptions = {row["modId"]: row for row in read_csv(exceptions_path)}

    if args.progress_band is not None:
        if not progression_path.is_file():
            print(f"ERROR: --progress-band requires {progression_path}", file=sys.stderr)
            sys.exit(1)
        progression = read_csv(progression_path)
        matches = [b for b in progression if int(b["progressBand"]) == args.progress_band]
        if not matches:
            print(
                f"ERROR: No DropQualityProgression row for progressBand={args.progress_band}",
                file=sys.stderr,
            )
            sys.exit(1)
        end_band = matches[0]
        allowed_min = int(end_band["allowedTierMin"])
        allowed_max = int(end_band["allowedTierMax"])
        progress_band_label = str(end_band["progressBand"])
    else:
        allowed_min, allowed_max = 1, 10**9
        progress_band_label = "all"

    out_rows: list[dict[str, str | int]] = []

    for affix in affixes:
        if affix.get("enabled", "TRUE").upper() != "TRUE":
            continue

        template = templates[affix["templateId"]]
        n = int(template["tierCount"])
        base_weight = int(float(affix["weight"]))
        mod_id = affix["modId"]
        ex = exceptions.get(mod_id)

        if ex and ex.get("forceTierCount"):
            n = int(ex["forceTierCount"])

        override_weight_map = parse_override_weights(ex["weightOverride"] if ex else "")
        hard_cap = int(ex["hardCap"]) if ex and ex.get("hardCap") else None

        for display_t in range(1, n + 1):
            if display_t < allowed_min or display_t > allowed_max:
                continue

            t_strength = display_tier_to_internal_strength(display_t, n)
            min_v, max_v = tier_value(template, t_strength, n)
            w = tier_weight(base_weight, template["weightCurveId"], t_strength)

            if override_weight_map:
                factor = override_weight_map.get(t_strength - 1, 1.0)
                w = max(1, int(round(base_weight * factor)))

            if hard_cap is not None:
                min_v = min(min_v, hard_cap)
                max_v = min(max_v, hard_cap)

            out_rows.append(
                {
                    "affixId": affix["affixId"].replace("_T1", f"_T{display_t}"),
                    "modId": mod_id,
                    "itemSlots": affix["itemSlots"],
                    "classSpecific": affix["classSpecific"],
                    "tier": display_t,
                    "weight": w,
                    "min": min_v,
                    "max": max_v,
                    "valueFormat": affix["valueFormat"],
                    "templateId": affix["templateId"],
                    "progressBand": progress_band_label,
                }
            )

    fieldnames = [
        "affixId",
        "modId",
        "itemSlots",
        "classSpecific",
        "tier",
        "weight",
        "min",
        "max",
        "valueFormat",
        "templateId",
        "progressBand",
    ]
    with out_path.open("w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(out_rows)

    mode = f"slice band {progress_band_label}" if args.progress_band is not None else "full catalog (all tiers)"
    print(
        f"Generated {len(out_rows)} rows -> {out_path} "
        f"({mode}; tier window: {allowed_min}-{allowed_max})"
    )


if __name__ == "__main__":
    main()
