---
title: Python API Reference
order: 5
---

# Python API Reference Plugin

The Python API plugin generates API reference documentation for Python
libraries, producing the same page layout as the built-in C# API docs.
Point it at a directory of `.py` source files and it extracts classes,
functions, dataclasses, enums, protocols, type annotations, and
Google-style docstrings — then renders fully navigable type pages with
signatures, parameter tables, return types, examples, and inheritance.

**Plugin ID:** `mokadocs-python-api`

**Requires:** Python 3.9+ on PATH (uses the standard library's `ast`
module — zero pip dependencies).

---

## Quick start

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      source: ../src/mylib
```

Run `mokadocs build`. The plugin:

1. Extracts a bundled Python analyzer script to a temp directory.
2. Invokes `python mokadocs_python_analyzer.py <source_dir> --output <json>`.
3. Deserializes the JSON into the same `ApiReference` model used by C# docs.
4. Generates an index page + one page per type using the shared `ApiPageRenderer`.

Pages appear at `/python-api/` by default (configurable via `routePrefix`).

---

## Configuration

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      # Required: path to the Python source directory (relative to mokadocs.yaml)
      source: ../src/mylib

      # Optional: display label for the nav section (default: "Python API")
      label: "API Reference"

      # Optional: URL route prefix (default: /python-api)
      routePrefix: /api

      # Optional: docstring format (default: google)
      docstringFormat: google

      # Optional: Python executable path (default: tries python3 then python)
      pythonPath: python3
```

### `source` (required)

Path to the directory containing your Python source files. Relative to the
`mokadocs.yaml` file. The analyzer recursively scans for `.py` files,
skipping:

- `__pycache__/`, `.git/`, `.venv/`, `venv/`, `node_modules/`
- Hidden directories (starting with `.`)
- Test files (`test_*.py`, `*_test.py`)
- Build/config files (`setup.py`, `conftest.py`, `noxfile.py`)

### `label`

Display name shown in the navigation sidebar and the index page heading.
Default: `"Python API"`.

### `routePrefix`

URL path prefix for all generated pages. Default: `/python-api`. Each type
page is at `/{routePrefix}/{module}/{typename}` (lowercased, dots become
slashes).

### `docstringFormat`

Docstring parsing format. Currently supported: `google` (default). The
Google format parses these section headers:

- `Args:` / `Arguments:` / `Parameters:` → parameter descriptions
- `Returns:` / `Return:` → return value description
- `Raises:` / `Throws:` → exception documentation
- `Examples:` / `Example:` → code examples
- `Attributes:` → dataclass/class attribute descriptions
- `Note:` / `Warning:` / `Todo:` → merged into remarks
- `See Also:` → cross-reference links

### `pythonPath`

Path to the Python executable. Default: tries `python3` first, falls back
to `python`. On Windows, `python3` is often a Microsoft Store alias that
returns exit code 9009 — the plugin handles this gracefully by trying the
next candidate.

Set this to a full path if Python isn't on your PATH:

```yaml
pythonPath: /usr/local/bin/python3.12
```

---

## What gets extracted

### Types

| Python construct | Mapped to | Detection |
|---|---|---|
| `class Foo:` | Class | Default for class nodes |
| `class Foo(Protocol):` or `class Foo(ABC):` | Interface | Base class is `Protocol` or `ABC` |
| `@dataclass class Foo:` | Record | Has `@dataclass` decorator |
| `class Color(Enum):` | Enum | Base class is `Enum`, `IntEnum`, `StrEnum`, `Flag` |
| Module-level functions | Static methods on a synthetic "module" type | Functions not inside a class |

### Members

| Python construct | Mapped to |
|---|---|
| `def __init__(self, ...)` | Constructor |
| `def method(self, ...)` | Method |
| `@staticmethod def foo(...)` | Method (static) |
| `@classmethod def foo(cls, ...)` | Method (static) |
| `@property def name(self)` | Property |
| `@abstractmethod def foo(self)` | Method (abstract) |
| `name: str` (class variable) | Field |
| `name: str = "default"` | Field (with default) |
| Enum values (`RED = "red"`) | Field (static) |

### Docstrings

Google-style docstrings are parsed into structured blocks:

```python
def divide(self, a: float, b: float) -> float:
    """Divide a by b.

    Args:
        a: Dividend.
        b: Divisor.

    Returns:
        The quotient of a and b.

    Raises:
        ZeroDivisionError: If b is zero.

    Examples:
        >>> calc.divide(10, 3)
        3.33
    """
```

This produces:
- Summary → "Divide a by b."
- Parameters table → a (float, "Dividend."), b (float, "Divisor.")
- Returns → "The quotient of a and b."
- Exceptions → ZeroDivisionError: "If b is zero."
- Examples → code block with the doctest

### Type annotations

All type annotations are preserved in signatures and parameter tables:

```python
def greet(name: str, greeting: str = "Hello") -> str:
```

→ Signature: `def greet(name: str, greeting: str='Hello') -> str`
→ Parameters: `name` (str), `greeting` (str, default: `'Hello'`)

`Optional[T]` is detected and marked as nullable. `*args` and `**kwargs`
are included in the parameter list.

### `__all__` exports

If a module defines `__all__`, only the listed names are included in the
generated docs. This lets you control the public API surface without
renaming private members.

### Decorators

Decorators (except `@property`, `@staticmethod`, `@classmethod`,
`@abstractmethod` which are mapped to member flags) are preserved as
attributes in the generated docs. `@deprecated` / `@deprecation` is
detected and shown as an obsolete warning.

---

## Output

The plugin generates:

```
_site/
  {routePrefix}/
    index.html                          ← index listing all modules + types
    {module}/
      {typename}/index.html             ← per-type page (same layout as C# API)
```

Each type page includes:
- Type header with badges (Class / Enum / Record / Interface, Abstract, Static)
- Type signature (Python syntax)
- Module path
- Summary from docstring
- Constructors (from `__init__`)
- Properties (from `@property`)
- Methods
- Fields / class variables
- Examples
- Inheritance (base classes + protocols)
- Type dependency graph (Mermaid diagram)

---

## Requirements

- **Python 3.9+** on the build machine (the analyzer uses `ast.unparse()`
  which was added in Python 3.9). No pip packages required.
- The Python source files must be **syntactically valid** — the analyzer
  uses `ast.parse()` which will skip files with syntax errors (logged as
  warnings, not build failures).

---

## Limitations

- **Google-style docstrings only** — NumPy-style and reStructuredText
  (Sphinx) formats are not currently parsed. Plain text docstrings are
  rendered as-is in the summary field.
- **No runtime analysis** — the plugin uses static AST parsing, not
  `import`. Dynamic attributes, monkey-patched methods, and metaclass-
  generated members are not discovered.
- **No cross-reference linking** — type names in annotations and
  docstrings are rendered as plain text, not hyperlinks to other type
  pages. This may be added in a future version.
- **Single source directory** — the plugin processes one `source:`
  directory per declaration. To document multiple packages, add multiple
  plugin entries with different `routePrefix` values.

---

## Example

Given this Python source:

```python
from dataclasses import dataclass
from enum import Enum
from typing import Optional

class Color(Enum):
    """Supported colors."""
    RED = "red"
    GREEN = "green"
    BLUE = "blue"

@dataclass
class DataPoint:
    """A data point with a label and value.

    Attributes:
        label: Human-readable label.
        value: Numeric value.
        unit: Optional unit of measurement.
    """
    label: str
    value: float
    unit: Optional[str] = None

class Calculator:
    """A simple calculator.

    Args:
        precision: Decimal places for results.
    """
    def __init__(self, precision: int = 2) -> None:
        self._precision = precision

    def add(self, a: float, b: float) -> float:
        """Add two numbers.

        Args:
            a: First operand.
            b: Second operand.

        Returns:
            The sum, rounded to the configured precision.
        """
        return round(a + b, self._precision)
```

With this config:

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      source: ./src
      label: "API Reference"
      routePrefix: /api
```

Produces pages at:
- `/api/` — index with Color (Enum), DataPoint (Record), Calculator (Class)
- `/api/{module}/color/` — enum with RED, GREEN, BLUE values
- `/api/{module}/datapoint/` — dataclass with label, value, unit fields
- `/api/{module}/calculator/` — class with constructor + add method
