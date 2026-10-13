"""Readers for the Ragnarok reference data (read-only inputs to the Runeheir importer).

- rAthena YAML databases (db/pre-re/*.yml): a small reader for their fixed layout (lists of maps, nested maps, "|" block
  scalars), so no YAML package is needed.
- The XileRetro client's System/ItemInfo.lub: a Lua table literal of item names, descriptions, slot counts and sprite ids.

Only stats and structure are used downstream; names and descriptions in the game are written fresh (Norse).
"""

import os
import re

SERVER_DB = os.environ.get("RH_RATHENA_DB", r"D:\Games\RagnarokServer\db\pre-re")
CLIENT_SYSTEM = os.environ.get(
    "RH_XILERETRO_SYSTEM", r"C:\Users\Aswanth Vijay\Downloads\XileRetro-Full_DGVoodoo_08.07.2026\XileRetro-Full\System")


# ---------------------------------------------------------------------------------------------- rAthena YAML
def _scalar(text):
    text = text.strip()
    if text == "":
        return None
    if text[0] in "\"'" and text[-1] == text[0]:
        return text[1:-1]
    low = text.lower()
    if low == "true":
        return True
    if low == "false":
        return False
    if re.fullmatch(r"-?\d+", text):
        return int(text)
    if re.fullmatch(r"-?\d+\.\d+", text):
        return float(text)
    return text


class _YamlReader:
    """Indentation-based reader for rAthena's YAML subset: maps, lists of maps or scalars, block scalars."""

    def __init__(self, lines):
        self.toks = []
        for raw in lines:
            if raw.strip() == "" or raw.lstrip().startswith("#"):
                self.toks.append((None, "", raw))
            else:
                self.toks.append((len(raw) - len(raw.lstrip(" ")), raw.strip(), raw))
        self.i = 0

    def peek(self):
        while self.i < len(self.toks) and self.toks[self.i][0] is None:
            self.i += 1
        return self.toks[self.i] if self.i < len(self.toks) else None

    def node(self, indent):
        tok = self.peek()
        if tok is None or tok[0] < indent:
            return None
        return self.seq(tok[0]) if tok[1].startswith("- ") or tok[1] == "-" else self.mapping(tok[0])

    def seq(self, indent):
        out = []
        while True:
            tok = self.peek()
            if tok is None or tok[0] != indent or not (tok[1].startswith("- ") or tok[1] == "-"):
                return out
            content = tok[1][2:] if tok[1].startswith("- ") else ""
            if re.match(r"^[A-Za-z0-9_\.\-]+\s*:(\s|$)", content):
                self.toks[self.i] = (indent + 2, content, tok[2])
                out.append(self.mapping(indent + 2))
            else:
                self.i += 1
                out.append(_scalar(content))

    def mapping(self, indent):
        out = {}
        while True:
            tok = self.peek()
            if tok is None or tok[0] != indent or tok[1].startswith("- "):
                return out
            key, _, rest = tok[1].partition(":")
            key, rest = key.strip(), rest.strip()
            self.i += 1
            if rest in ("|", ">", "|-", ">-"):
                buf = []
                while self.i < len(self.toks):
                    ind, _, raw = self.toks[self.i]
                    if ind is not None and ind <= indent:
                        break
                    buf.append(raw[indent + 2:] if ind is not None else "")
                    self.i += 1
                out[key] = "\n".join(buf).strip("\n")
            elif rest == "":
                child = self.peek()
                if child is not None and child[0] > indent:
                    out[key] = self.node(child[0])
                elif child is not None and child[0] == indent and child[1].startswith("- "):
                    out[key] = self.seq(indent)
                else:
                    out[key] = None
            else:
                out[key] = _scalar(rest)


def read_yaml(path):
    """Parses an rAthena database file into its Body list (each entry a dict)."""
    with open(path, encoding="utf-8", errors="replace") as f:
        lines = f.read().splitlines()
    body = []
    inside = False
    for raw in lines:
        if not inside:
            inside = raw.startswith("Body:")
            continue
        if raw and not raw[0].isspace() and not raw.startswith("#") and not raw.startswith("-"):
            break
        body.append(raw)
    reader = _YamlReader(body)
    first = reader.peek()
    return reader.node(first[0]) if first else []


def read_items():
    """All pre-renewal items by id: equipment, usable and etc files."""
    items = {}
    for name in ("item_db_equip.yml", "item_db_usable.yml", "item_db_etc.yml"):
        for entry in read_yaml(os.path.join(SERVER_DB, name)):
            if "Id" in entry:
                entry["_file"] = name
                items[int(entry["Id"])] = entry
    return items


# ---------------------------------------------------------------------------------------------- the client's item table
_STRING = r'"((?:[^"\\]|\\.)*)"'


def read_item_info(path=None):
    """Parses System/ItemInfo.lub into {id: {name, unidentified, description[], slots, view, costume}}."""
    path = path or os.path.join(CLIENT_SYSTEM, "ItemInfo.lub")
    with open(path, "rb") as f:
        text = f.read().decode("latin-1")
    items = {}
    # Each entry starts with "[id] = {" on its own line; its body runs to the next entry.
    headers = list(re.finditer(r"^[ \t]*\[(\d+)\]\s*=\s*\{", text, re.M))
    for n, match in enumerate(headers):
        item_id = int(match.group(1))
        body = text[match.end():headers[n + 1].start() if n + 1 < len(headers) else len(text)]

        # (?<![A-Za-z]) keeps "identified..." from matching inside "unidentified...".
        def field(name):
            m = re.search(r"(?<![A-Za-z])" + name + r"\s*=\s*" + _STRING, body)
            return m.group(1) if m else ""

        def number(name):
            m = re.search(r"(?<![A-Za-z])" + name + r"\s*=\s*(-?\d+)", body)
            return int(m.group(1)) if m else 0

        desc = []
        m = re.search(r"(?<![A-Za-z])identifiedDescriptionName\s*=\s*\{(.*?)\n\s*\}", body, re.S)
        if m:
            desc = [re.sub(r"\^[0-9A-Fa-f]{6}", "", s) for s in re.findall(_STRING, m.group(1))]
        costume = re.search(r"costume\s*=\s*true", body) is not None
        items[item_id] = {
            "name": field("identifiedDisplayName"),
            "unidentified": field("unidentifiedDisplayName"),
            "description": desc,
            "slots": number("slotCount"),
            "view": number("ClassNum"),
            "costume": costume,
        }
    return items


def description_field(desc, label):
    """'Weight' -> the value after 'Weight:' in a description, or None."""
    for line in desc:
        m = re.match(r"\s*" + re.escape(label) + r"\s*:\s*(.+)$", line, re.I)
        if m:
            return m.group(1).strip()
    return None
