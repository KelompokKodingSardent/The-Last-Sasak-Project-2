#!/usr/bin/env python3
"""Create an AI-readable audit of a Unity project.

Place this script in the Unity project root (next to Assets, Packages and
ProjectSettings), then run: python audit_unity_for_ai.py

The script does not modify project files. It writes _AI_AUDIT/ and
_AI_AUDIT.zip in the project root.
"""

from __future__ import annotations

import os
import re
import zipfile
from collections import Counter
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT_DIR = ROOT / "_AI_AUDIT"
ZIP_PATH = ROOT / "_AI_AUDIT.zip"

# Generated/cache folders that should not be audited as project source.
EXCLUDED_DIRS = {
    "library", "temp", "obj", "logs", "build", "builds", "buildcache",
    ".git", ".vs", ".idea", ".vscode", "__pycache__", ".venv",
    "venv", "node_modules", "_ai_audit",
}

# Never include the contents of credential-like files in an AI audit.
SENSITIVE_NAMES = {".env", ".env.local", ".env.production", "secrets", "credentials"}
SENSITIVE_SUFFIXES = {".keystore", ".jks", ".p12", ".pfx", ".pem", ".key"}

CODE_EXTS = {
    ".cs", ".shader", ".compute", ".hlsl", ".cginc", ".glsl",
    ".uxml", ".uss", ".js", ".ts", ".tsx", ".jsx", ".py",
    ".ps1", ".bat", ".sql", ".csproj", ".sln", ".props", ".targets",
}

UNITY_DATA_EXTS = {
    ".unity", ".prefab", ".asset", ".mat", ".controller",
    ".overridecontroller", ".anim", ".mask", ".inputactions",
    ".asmdef", ".asmref", ".json", ".xml", ".txt", ".md",
    ".yml", ".yaml", ".rsp", ".gradle", ".properties",
    ".config", ".ini", ".toml", ".csv", ".shadergraph",
    ".shadersubgraph", ".terrainlayer", ".physicmaterial",
    ".rendertexture", ".playable", ".guiskin",
}

TEXT_EXTS = CODE_EXTS | UNITY_DATA_EXTS
MAX_FILE_BYTES = 500_000
MAX_PART_CHARS = 130_000
GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def is_sensitive(path: Path) -> bool:
    name = path.name.lower()
    return (
        name in SENSITIVE_NAMES
        or name.startswith(".env.")
        or path.suffix.lower() in SENSITIVE_SUFFIXES
        or "service-account" in name
        or "credential" in name
        or "secret" in name
    )


def collect_files() -> list[Path]:
    found: list[Path] = []
    for current, dirs, filenames in os.walk(ROOT, topdown=True, followlinks=False):
        current_path = Path(current)
        dirs[:] = [
            d for d in dirs
            if d.lower() not in EXCLUDED_DIRS
            and not (current_path == ROOT and d == OUT_DIR.name)
        ]
        for filename in filenames:
            path = current_path / filename
            if path == ZIP_PATH or path == Path(__file__):
                continue
            found.append(path)
    return sorted(found, key=lambda p: rel(p).lower())


def read_text(path: Path) -> str | None:
    try:
        if path.stat().st_size > MAX_FILE_BYTES:
            return None
        raw = path.read_bytes()
        if b"\x00" in raw[:4096]:
            return None
        return raw.decode("utf-8-sig", errors="replace")
    except (OSError, PermissionError):
        return None


def get_guid_map(files: list[Path]) -> dict[str, str]:
    result: dict[str, str] = {}
    for path in files:
        if path.suffix.lower() != ".meta" or is_sensitive(path):
            continue
        try:
            # Unity .meta GUID is near the top; reading a small prefix is enough.
            content = path.read_text(encoding="utf-8", errors="replace")[:5000]
            match = GUID_RE.search(content)
            if match:
                asset_path = path.with_suffix("")
                result[rel(asset_path)] = match.group(1).lower()
                result[rel(path)] = match.group(1).lower()
        except OSError:
            pass
    return result


def language_for(path: Path) -> str:
    return {
        ".cs": "csharp", ".shader": "hlsl", ".compute": "hlsl",
        ".hlsl": "hlsl", ".cginc": "hlsl", ".glsl": "glsl",
        ".json": "json", ".asmdef": "json", ".asmref": "json",
        ".unity": "yaml", ".prefab": "yaml", ".asset": "yaml",
        ".mat": "yaml", ".controller": "yaml", ".anim": "yaml",
        ".overridecontroller": "yaml", ".mask": "yaml",
        ".yml": "yaml", ".yaml": "yaml", ".xml": "xml",
        ".uxml": "xml", ".uss": "css", ".md": "markdown",
        ".py": "python", ".ps1": "powershell", ".sql": "sql",
        ".js": "javascript", ".ts": "typescript", ".tsx": "tsx",
        ".jsx": "jsx", ".gradle": "groovy", ".csv": "csv",
    }.get(path.suffix.lower(), "text")


def write_dump(label: str, records: list[tuple[str, str, str]]) -> list[Path]:
    """Write records into bounded Markdown parts; split exceptionally large files."""
    if not records:
        return []

    output_paths: list[Path] = []
    part_number = 1
    current = f"# {label} — Part {part_number}\n\n"

    def flush() -> None:
        nonlocal current, part_number
        if current.strip().endswith(f"Part {part_number}"):
            return
        out_path = OUT_DIR / f"{label.lower().replace(' ', '_')}_{part_number:03d}.md"
        out_path.write_text(current, encoding="utf-8")
        output_paths.append(out_path)
        part_number += 1
        current = f"# {label} — Part {part_number}\n\n"

    for file_path, content, language in records:
        segment_size = MAX_PART_CHARS - 1500
        pieces = [content[i:i + segment_size] for i in range(0, len(content), segment_size)] or [""]
        for index, piece in enumerate(pieces, start=1):
            segment_note = f" (segment {index}/{len(pieces)})" if len(pieces) > 1 else ""
            block = (
                f"\n## `{file_path}`{segment_note}\n\n"
                f"```{language}\n{piece}\n```\n"
            )
            if len(current) + len(block) > MAX_PART_CHARS and len(current) > 100:
                flush()
            current += block
    flush()
    return output_paths


def main() -> None:
    OUT_DIR.mkdir(exist_ok=True)
    # Remove only the previous generated audit files, not anything else.
    for old_file in OUT_DIR.glob("*.md"):
        old_file.unlink(missing_ok=True)

    files = collect_files()
    guid_map = get_guid_map(files)
    extension_counts = Counter((p.suffix.lower() or "[no extension]") for p in files)
    scene_paths = [rel(p) for p in files if p.suffix.lower() == ".unity"]
    script_paths = [p for p in files if p.suffix.lower() == ".cs"]

    inventory_lines: list[str] = []
    skipped_sensitive: list[str] = []
    oversized: list[str] = []
    unreadable: list[str] = []
    code_records: list[tuple[str, str, str]] = []
    data_records: list[tuple[str, str, str]] = []

    for path in files:
        relative = rel(path)
        try:
            size = path.stat().st_size
        except OSError:
            size = -1

        if is_sensitive(path):
            inventory_lines.append(f"- `{relative}` — {size:,} bytes — CONTENT EXCLUDED (potential secret)".replace(",", "."))
            skipped_sensitive.append(relative)
            continue

        guid = guid_map.get(relative)
        guid_text = f" — GUID `{guid}`" if guid else ""
        inventory_lines.append(f"- `{relative}` — {size:,} bytes{guid_text}".replace(",", "."))

        suffix = path.suffix.lower()
        if suffix not in TEXT_EXTS:
            continue
        if size > MAX_FILE_BYTES:
            oversized.append(relative)
            continue
        content = read_text(path)
        if content is None:
            unreadable.append(relative)
            continue

        record = (relative, content, language_for(path))
        if suffix in CODE_EXTS:
            code_records.append(record)
        else:
            data_records.append(record)

    project_version = "Tidak ditemukan"
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    if version_file.exists():
        project_version = read_text(version_file) or "Ada, tetapi tidak dapat dibaca"
        project_version = " ".join(project_version.strip().splitlines())

    top_folders = sorted(
        [p.name for p in ROOT.iterdir() if p.is_dir() and p.name.lower() not in EXCLUDED_DIRS and p != OUT_DIR],
        key=str.lower,
    )
    ext_lines = [f"- `{ext}`: {count}" for ext, count in sorted(extension_counts.items())]
    scene_lines = [f"- `{s}`" for s in scene_paths] or ["- Tidak ditemukan file `.unity`."]
    inventory = "\n".join(inventory_lines) if inventory_lines else "(Tidak ada file ditemukan.)"

    overview = f"""# Unity Project Audit — Project Map

Generated: {datetime.now().astimezone().isoformat(timespec='seconds')}

## Project information
- Project root: `{ROOT}`
- Unity version: `{project_version}`
- Top-level folders: {', '.join(f'`{x}`' for x in top_folders) or '(none)'}
- Total inventoried files: **{len(files)}**
- C# scripts found: **{len(script_paths)}**
- Unity scenes found: **{len(scene_paths)}**

## Important interpretation notes for the AI
- This audit is a snapshot of the project at the generation time.
- Paths and file sizes are inventoried below, including binary assets; binary file contents are not embedded.
- C# and shader source files are in `code_*.md` files.
- Scenes, prefabs, ScriptableObjects, materials, assembly definitions, settings and text configuration are in `unity_data_*.md` files when their extensions are supported and their size is within the limit.
- `.meta` files are inventoried and their GUIDs are recorded where available. Their full contents are not dumped.
- Generated folders excluded from the scan: {', '.join(f'`{x}`' for x in sorted(EXCLUDED_DIRS))}.
- Potentially sensitive files are listed but their contents are intentionally excluded.
- Text files larger than {MAX_FILE_BYTES:,} bytes are not embedded; check the skipped lists below.

## Scene files
{chr(10).join(scene_lines)}

## File counts by extension
{chr(10).join(ext_lines)}

## Complete file path inventory
{inventory}

## Contents not embedded
### Potentially sensitive files
{chr(10).join(f'- `{x}`' for x in skipped_sensitive) or '- None detected.'}

### Text files over the size limit
{chr(10).join(f'- `{x}`' for x in oversized) or '- None.'}

### Text files that could not be read as text
{chr(10).join(f'- `{x}`' for x in unreadable) or '- None.'}
"""
    (OUT_DIR / "00_project_map.md").write_text(overview, encoding="utf-8")

    code_parts = write_dump("code", code_records)
    data_parts = write_dump("unity_data", data_records)

    readme = f"""# AI Audit Bundle — Unity

Audit ini dibuat otomatis dari project Unity pada `{datetime.now().astimezone().isoformat(timespec='seconds')}`.

## Urutan membaca
1. Baca `00_project_map.md` untuk memahami versi Unity, daftar Scene, jumlah file, dan seluruh path.
2. Baca semua `code_*.md` untuk memahami implementasi C#, shader, dan source code lain.
3. Baca semua `unity_data_*.md` untuk memahami konfigurasi, Scene, Prefab, ScriptableObject, Material, dan data serialisasi Unity.
4. Cocokkan referensi asset berdasarkan GUID yang tercantum pada project map.

## Batasan audit
Audit ini tidak memasukkan isi file binary seperti FBX, PNG, JPG, WAV, MP3, DLL, EXE, dan file model/tekstur lainnya. File tersebut tetap muncul pada daftar path beserta ukuran dan GUID bila tersedia. Folder cache/build dikecualikan. File yang terlihat berpotensi menyimpan credential tidak disalin.

## Instruksi untuk AI
Perlakukan audit ini sebagai gambaran struktur project, bukan izin untuk mengasumsikan perilaku yang tidak terlihat di source. Saat memberi saran perubahan:
- sebutkan path file yang tepat;
- periksa apakah class, method, field, dan reference memang ada pada audit;
- jangan menyarankan membuat ulang sistem yang sudah tersedia;
- jelaskan file mana yang perlu diubah dan dampaknya ke file lain;
- bila informasi tidak tersedia, nyatakan bahwa bagian tersebut belum dapat diverifikasi.

## Ringkasan dump
- Code files embedded: {len(code_records)}; output parts: {len(code_parts)}.
- Unity/config text files embedded: {len(data_records)}; output parts: {len(data_parts)}.
"""
    (OUT_DIR / "README_FOR_AI.md").write_text(readme, encoding="utf-8")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(OUT_DIR.glob("*.md")):
            archive.write(path, arcname=f"_AI_AUDIT/{path.name}")

    print("Unity AI audit selesai.")
    print(f"Project root : {ROOT}")
    print(f"Files scanned: {len(files)}")
    print(f"C# scripts   : {len(script_paths)}")
    print(f"Scenes       : {len(scene_paths)}")
    print(f"Code dumps   : {len(code_records)} file(s) -> {len(code_parts)} part(s)")
    print(f"Unity dumps  : {len(data_records)} file(s) -> {len(data_parts)} part(s)")
    print(f"Output folder: {OUT_DIR}")
    print(f"ZIP bundle   : {ZIP_PATH}")
    print("\nPeriksa README_FOR_AI.md dan daftar file sensitif sebelum mengunggah ZIP ke AI.")


if __name__ == "__main__":
    main()
