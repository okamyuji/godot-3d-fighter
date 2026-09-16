#!/usr/bin/env sh
# 文書とソースの機械検査。リポジトリ全体の*.mdと*.csで和文と英数字の境界スペースを、
# docs/配下のMarkdownでADRの必須節、先送り語、文末コロンを検出する。1件でも検出すれば終了コード1。
set -eu
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
exec python3 - "$ROOT" "$@" <<'PY'
import re, sys, pathlib

root = pathlib.Path(sys.argv[1])
SKIP_DIRS = {".git", ".godot", "bin", "obj", "StrykerOutput", "TestResults"}
targets = [pathlib.Path(p) for p in sys.argv[2:]] or [root]

def walk(t):
    if t.is_file():
        yield t
        return
    for f in t.rglob("*"):
        if f.suffix in (".md", ".cs") and not (SKIP_DIRS & set(f.relative_to(t).parts)):
            yield f

files = sorted({f for t in targets for f in walk(t)})

ADR_SECTIONS = ["## 状態", "## 背景", "## 決定", "## 影響"]
BANNED = [r"TBD", r"TODO", r"未定(?!義)", r"後で検討", r"対応予定", r"確信度", r"推奨は", r"不変条件", r"帰結", r"デシンク"]
JA = "぀-ヿ一-鿿"
# 和文と英数字の間の半角スペース。ラベルや見出しも例外にしない。
BOUNDARY = re.compile(rf"[{JA}] [A-Za-z0-9`]|[A-Za-z0-9`] [{JA}]")
findings = []

def check(path):
    rel = path.relative_to(root) if path.is_relative_to(root) else path
    text = path.read_text(encoding="utf-8")
    is_doc = path.suffix == ".md" and "docs" in rel.parts
    if is_doc and "adr" in rel.parts and path.name != "README.md":
        for s in ADR_SECTIONS:
            if f"\n{s}\n" not in f"\n{text}":
                findings.append(("High", rel, 0, f"ADR必須節 '{s}' が無い"))
    in_fence = False
    for n, line in enumerate(text.splitlines(), 1):
        if line.strip().startswith("```"):
            in_fence = not in_fence
            continue
        if in_fence or line.startswith("    "):
            continue
        # インラインコードとURLは英数字1文字に置き換え、前後の境界だけを検査する
        plain = re.sub(r"`[^`]*`", "C", line)
        plain = re.sub(r"https?://\S+", "U", plain)
        if BOUNDARY.search(plain):
            findings.append(("Medium", rel, n, "和文と英数字の境界に半角スペース"))
        if not is_doc:
            continue
        for b in BANNED:
            if re.search(b, plain):
                findings.append(("High", rel, n, f"先送り語または過程語 '{b}'"))
        if re.search(r"[:：]\s*$", plain) and not plain.lstrip().startswith(("|", "-", "*", "#")):
            findings.append(("Medium", rel, n, "文末がコロンで終わる"))

for f in files:
    check(f)
counts = {s: 0 for s in ("Critical", "High", "Medium", "Low")}
for sev, rel, n, msg in findings:
    counts[sev] += 1
    print(f"[{sev}] {rel}:{n} {msg}")
print("doclint:", " ".join(f"{k} {v}" for k, v in counts.items()))
sys.exit(1 if findings else 0)
PY
