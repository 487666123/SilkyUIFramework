#!/usr/bin/env python3
"""查找混用换行符的 C# 文件，并可选择统一为 CRLF。"""

from __future__ import annotations

import argparse
import os
import sys
from dataclasses import dataclass
from pathlib import Path

try:
    from rich.console import Console
    from rich.prompt import Prompt
    from rich.panel import Panel
    from rich.table import Table
except ImportError:  # pragma: no cover - depends on local environment
    print(
        "此脚本需要 'rich' 包。请先安装：py -m pip install rich",
        file=sys.stderr,
    )
    raise SystemExit(1)


NEWLINE_LABELS = {
    "crlf": "CRLF",
    "lf": "LF",
    "cr": "CR",
}

DEFAULT_EXCLUDED_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    "bin",
    "obj",
    "node_modules",
    "packages",
}

DEFAULT_TABLE_LIMIT = 200


@dataclass(frozen=True)
class NewlineStats:
    path: Path
    crlf: int
    lf: int
    cr: int

    @property
    def active_styles(self) -> tuple[str, ...]:
        return tuple(
            key
            for key, value in (("crlf", self.crlf), ("lf", self.lf), ("cr", self.cr))
            if value > 0
        )

    @property
    def is_mixed(self) -> bool:
        return len(self.active_styles) > 1

    @property
    def category(self) -> str:
        return " + ".join(NEWLINE_LABELS[key] for key in self.active_styles)

    @property
    def total(self) -> int:
        return self.crlf + self.lf + self.cr


def count_newlines(data: bytes) -> tuple[int, int, int]:
    crlf = lf = cr = 0
    index = 0

    while index < len(data):
        byte = data[index]
        if byte == 0x0D:  # CR
            if index + 1 < len(data) and data[index + 1] == 0x0A:
                crlf += 1
                index += 2
            else:
                cr += 1
                index += 1
        elif byte == 0x0A:  # LF
            lf += 1
            index += 1
        else:
            index += 1

    return crlf, lf, cr


def iter_cs_files(root: Path) -> list[Path]:
    paths: list[Path] = []

    def handle_walk_error(error: OSError) -> None:
        return None

    for current_root, dirnames, filenames in os.walk(root, onerror=handle_walk_error):
        dirnames[:] = [
            dirname
            for dirname in dirnames
            if dirname not in DEFAULT_EXCLUDED_DIRS
        ]

        current_path = Path(current_root)
        for filename in filenames:
            if filename.endswith(".cs"):
                paths.append(current_path / filename)

    return sorted(paths)


def scan_cs_files(root: Path) -> list[NewlineStats]:
    results: list[NewlineStats] = []

    for path in iter_cs_files(root):
        if not path.is_file():
            continue

        try:
            crlf, lf, cr = count_newlines(path.read_bytes())
        except OSError:
            continue

        stats = NewlineStats(path=path, crlf=crlf, lf=lf, cr=cr)
        if stats.is_mixed:
            results.append(stats)

    return results


def normalize_newlines_to_crlf(data: bytes) -> bytes:
    normalized = bytearray()
    index = 0

    while index < len(data):
        byte = data[index]
        if byte == 0x0D:  # CR
            if index + 1 < len(data) and data[index + 1] == 0x0A:
                normalized.extend(b"\r\n")
                index += 2
            else:
                normalized.extend(b"\r\n")
                index += 1
        elif byte == 0x0A:  # LF
            normalized.extend(b"\r\n")
            index += 1
        else:
            normalized.append(byte)
            index += 1

    return bytes(normalized)


def normalize_files_to_crlf(results: list[NewlineStats]) -> int:
    changed = 0

    for item in results:
        original = item.path.read_bytes()
        normalized = normalize_newlines_to_crlf(original)
        if normalized != original:
            item.path.write_bytes(normalized)
            changed += 1

    return changed


def build_table(title: str, rows: list[NewlineStats], root: Path) -> Table:
    table = Table(title=title, show_lines=False, expand=True)
    table.add_column("#", justify="right", style="dim", no_wrap=True)
    table.add_column("文件", overflow="fold")
    table.add_column("CRLF", justify="right", style="cyan", no_wrap=True)
    table.add_column("LF", justify="right", style="green", no_wrap=True)
    table.add_column("CR", justify="right", style="magenta", no_wrap=True)
    table.add_column("总数", justify="right", style="bold", no_wrap=True)

    for row_number, item in enumerate(rows, start=1):
        table.add_row(
            str(row_number),
            str(item.path.relative_to(root)),
            str(item.crlf),
            str(item.lf),
            str(item.cr),
            str(item.total),
        )

    return table


def render_results(console: Console, root: Path, results: list[NewlineStats], table_limit: int | None) -> None:
    if not results:
        console.print(
            Panel.fit(
                f"在 [bold]{root}[/bold] 下没有找到混用换行符的 .cs 文件。",
                title="换行符扫描",
                border_style="green",
            )
        )
        return

    console.print(
        Panel.fit(
            f"在 [bold]{root}[/bold] 下找到 [bold red]{len(results)}[/bold red] 个混用换行符的 .cs 文件。",
            title="换行符扫描",
            border_style="yellow",
        )
    )

    visible_results = results if table_limit is None else results[:table_limit]
    hidden_count = len(results) - len(visible_results)

    grouped: dict[str, list[NewlineStats]] = {}
    for item in visible_results:
        grouped.setdefault(item.category, []).append(item)

    category_order = {
        "CRLF + LF": 0,
        "CRLF + CR": 1,
        "LF + CR": 2,
        "CRLF + LF + CR": 3,
    }

    for category, rows in sorted(grouped.items(), key=lambda item: category_order.get(item[0], 99)):
        console.print()
        console.print(build_table(f"混用类型：{category}", rows, root))

    if hidden_count > 0:
        console.print()
        console.print(
            f"[yellow]还有 {hidden_count} 个结果未显示。使用 --all 可显示全部表格行。[/yellow]"
        )


def prompt_for_action(console: Console) -> str:
    console.print()
    console.print("[bold]操作选项[/bold]")
    console.print("  [cyan]1[/cyan]. 将表格中的文件统一为 CRLF")
    console.print("  [cyan]2[/cyan]. 退出，不修改文件")
    return Prompt.ask("请选择操作", choices=["1", "2"], default="2")


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="扫描 .cs 文件中的混用换行符，并可选择统一为 CRLF。"
    )
    parser.add_argument(
        "root",
        nargs="?",
        default=".",
        type=Path,
        help="要扫描的根目录，默认为当前目录。",
    )
    parser.add_argument(
        "--fix-crlf",
        action="store_true",
        help="不弹出菜单，直接将混用换行符的 .cs 文件统一为 CRLF。",
    )
    parser.add_argument(
        "--no-prompt",
        action="store_true",
        help="只展示扫描结果，不弹出菜单，也不修改文件。",
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="显示全部表格行。默认最多显示前 200 行，避免大目录输出过多。",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    root = args.root.resolve()
    console = Console()

    if not root.exists() or not root.is_dir():
        console.print(f"[bold red]无效目录：[/bold red] {root}")
        return 2

    results = scan_cs_files(root)
    table_limit = None if args.all else DEFAULT_TABLE_LIMIT
    render_results(console, root, results, table_limit)

    if not results:
        return 0

    should_fix = args.fix_crlf
    if not should_fix and not args.no_prompt:
        should_fix = prompt_for_action(console) == "1"

    if should_fix:
        changed = normalize_files_to_crlf(results)
        console.print()
        console.print(f"[bold green]已将 {changed} 个文件统一为 CRLF。[/bold green]")
        remaining = scan_cs_files(root)
        if remaining:
            console.print(f"[bold red]仍有 {len(remaining)} 个文件存在混用换行符。[/bold red]")
            return 1
        console.print("[bold green]已无混用换行符的 .cs 文件。[/bold green]")
        return 0

    console.print("[dim]已退出，未修改文件。[/dim]")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
