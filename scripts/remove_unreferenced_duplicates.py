#!/usr/bin/env python3
import argparse
import fnmatch
import os
from pathlib import Path
import xml.etree.ElementTree as ET


def parse_sln_projects(sln_path: Path):
    projects = []
    for line in sln_path.read_text(encoding="utf-8", errors="ignore").splitlines():
        line = line.strip()
        if line.startswith("Project("):
            parts = line.split(",")
            if len(parts) >= 2:
                rel = parts[1].strip().strip('"')
                if rel.lower().endswith(".csproj"):
                    projects.append((sln_path.parent / rel).resolve())
    return projects


def _strip_ns(tag: str):
    return tag.split("}", 1)[-1]


def parse_csproj_compile_files(csproj_path: Path):
    tree = ET.parse(csproj_path)
    root = tree.getroot()

    explicit_includes = []
    remove_patterns = []
    sdk_style = "Sdk" in root.attrib or root.tag.endswith("Project")

    for elem in root.iter():
        tag = _strip_ns(elem.tag)
        if tag == "Compile":
            inc = elem.attrib.get("Include")
            rem = elem.attrib.get("Remove")
            if inc:
                explicit_includes.append(inc)
            if rem:
                remove_patterns.append(rem)

    proj_dir = csproj_path.parent
    result = set()

    if explicit_includes:
        for pat in explicit_includes:
            matches = list(proj_dir.glob(pat))
            if matches:
                for m in matches:
                    if m.is_file() and m.suffix.lower() == ".cs":
                        result.add(m.resolve())
            else:
                p = (proj_dir / pat).resolve()
                if p.is_file() and p.suffix.lower() == ".cs":
                    result.add(p)
    elif sdk_style:
        for p in proj_dir.rglob("*.cs"):
            rel = p.relative_to(proj_dir).as_posix()
            if rel.startswith("bin/") or rel.startswith("obj/"):
                continue
            result.add(p.resolve())

    if remove_patterns and result:
        filtered = set()
        for f in result:
            rel = f.relative_to(proj_dir).as_posix()
            if any(fnmatch.fnmatch(rel, pat.replace("\\", "/")) for pat in remove_patterns):
                continue
            filtered.add(f)
        result = filtered

    return result


def main():
    ap = argparse.ArgumentParser(description="Remove duplicate C# files that are not used by projects.")
    ap.add_argument("--root", default=".", help="Repository root")
    ap.add_argument("--exclude-project", action="append", default=[], help="Project path fragment to exclude")
    ap.add_argument("--preferred-dir", default="KuBIT.DAL", help="Directory fragment preferred for keeping duplicates")
    ap.add_argument("--apply", action="store_true", help="Actually delete files. Without this, runs in dry-run mode.")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    sln_files = list(root.glob("*.sln"))

    projects = []
    if sln_files:
        for sln in sln_files:
            projects.extend(parse_sln_projects(sln))
    else:
        projects = list(root.rglob("*.csproj"))

    projects = [p for p in projects if p.exists()]
    if args.exclude_project:
        ex = [e.lower().replace("\\", "/") for e in args.exclude_project]
        projects = [
            p for p in projects
            if not any(fragment in p.as_posix().lower() for fragment in ex)
        ]

    used_files = set()
    for proj in projects:
        try:
            used_files.update(parse_csproj_compile_files(proj))
        except Exception as e:
            print(f"[WARN] Failed parsing {proj}: {e}")

    all_cs = [p.resolve() for p in root.rglob("*.cs") if ".git/" not in p.as_posix()]
    by_name = {}
    for f in all_cs:
        by_name.setdefault(f.name.lower(), []).append(f)

    duplicates = {k: v for k, v in by_name.items() if len(v) > 1}
    to_delete = []

    pref = args.preferred_dir.lower().replace("\\", "/")

    for _, files in duplicates.items():
        used = [f for f in files if f in used_files]
        unused = [f for f in files if f not in used_files]

        keep = None
        preferred_used = [f for f in used if pref in f.as_posix().lower()]
        if preferred_used:
            keep = preferred_used[0]
        elif used:
            keep = used[0]
        else:
            preferred_any = [f for f in files if pref in f.as_posix().lower()]
            keep = preferred_any[0] if preferred_any else files[0]

        for f in files:
            if f == keep:
                continue
            if f in unused:
                to_delete.append(f)

    if not to_delete:
        print("No unreferenced duplicate .cs files found.")
        return

    for f in sorted(to_delete):
        rel = f.relative_to(root)
        if args.apply:
            f.unlink(missing_ok=True)
            print(f"DELETED: {rel}")
        else:
            print(f"WOULD DELETE: {rel}")


if __name__ == "__main__":
    main()
