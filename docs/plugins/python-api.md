---
title: Python API Reference
order: 5
---

# Python API Reference Plugin

The Python API plugin generates API reference documentation for Python
libraries, using the same page renderer as the built-in C# API docs.
Point it at a directory of `.py` source files and it extracts classes,
functions, dataclasses, enums, protocols, type annotations, and
Google-style docstrings, then renders type pages with member tables,
parameter tables, return values, exceptions, and inheritance.

**Plugin ID:** `mokadocs-python-api`

**Requires:** Python 3.9 or later on `PATH` (the analyzer uses only the
standard library's `ast` module, no pip packages).

---

## Quick start

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      source: ../src/mylib
```

Run `mokadocs build`. The plugin:

1. Extracts its bundled analyzer script (`mokadocs_python_analyzer.py`) to a uniquely named file in the system temp directory, which it deletes afterwards.
2. Runs the script with Python: `python <script> <source_dir> --output <json> --format google`.
3. Deserializes the JSON into the same `ApiReference` model used by C# docs.
4. Generates an index page plus one page per type with the shared `ApiPageRenderer`.

Pages appear at `/python-api` by default (configurable via `routePrefix`).

Problems show up as build diagnostics: a missing `source` option, a source
directory that doesn't exist, Python not being found, or the analyzer failing
are errors (so `mokadocs build` exits with code 1). Files with syntax errors and
a source directory with no public types are warnings.

---

## Configuration

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      # Required: path to the Python source directory (relative to mokadocs.yaml)
      source: ../src/mylib

      # Optional: title of the index page (default: "Python API")
      label: "Python Reference"

      # Optional: URL route prefix (default: /python-api)
      routePrefix: /python

      # Optional: docstring format (default and only supported value: google)
      docstringFormat: google

      # Optional: Python executable path (default: tries python3 then python)
      pythonPath: python3
```

### `source` (required)

Path to the directory containing your Python source files. Relative to the
`mokadocs.yaml` file. The analyzer recursively scans for `.py` files,
skipping:

- `__pycache__/`, `.git/`, `.venv/`, `venv/`, `node_modules/`, `.tox/`, `.mypy_cache/`
- Any file or directory whose name starts with `.`
- Test files (`test_*.py`, `*_test.py`)
- Build/config files (`setup.py`, `conftest.py`, `noxfile.py`, `fabfile.py`)

Module names come from file paths relative to `source`. With
`source: ./src/mylib`, the file `src/mylib/calculator.py` is the module
`calculator` and `src/mylib/utils/io.py` is `utils.io`. To get the package
name into module names, point `source` at the parent directory. A package's
`__init__.py` belongs to the package's module (`utils/__init__.py` is
`utils`), but an `__init__.py` directly inside `source` becomes a module named
`__init__`.

### `label`

Title of the index page. When `mokadocs.yaml` has no `nav:` section, it is
also the sidebar section label. Default: `"Python API"`.

### `routePrefix`

URL path prefix for all generated pages. Default: `/python-api`. A missing
leading `/` is added. Routes are lowercase:

- Index: `{routePrefix}`
- A class, enum, dataclass or protocol: `{routePrefix}/{module path}/{name}`
- A module's functions: `{routePrefix}/{module path}`

The module path is the module name with dots replaced by slashes.

Don't use `/api` on a site that also lists C# projects in `content.projects`.
The C# API reference uses `/api` for its index page, the two index pages
overwrite each other, and the build warns about the shared route.

Generated pages appear in the sidebar automatically only when `mokadocs.yaml`
has no `nav:` section. With one, add an item for the prefix:

```yaml
nav:
  - label: "Python Reference"
    path: /python
```

### `docstringFormat`

Docstring parsing format. `google` (the default) is the only supported value;
any other value makes the analyzer exit with an error, which fails the build.
The Google format recognizes these section headers, in any letter case, each
on its own line:

- `Args:` / `Arguments:` / `Parameters:` / `Params:` → parameter descriptions
- `Returns:` / `Return:` → return value description
- `Raises:` / `Throws:` → exception documentation
- `Examples:` / `Example:` → examples
- `Attributes:` → attribute descriptions, used as the remarks when there are no other remarks
- `Note:` / `Warning:` / `Todo:` (or their plurals) → appended to the remarks
- `See Also:` → related names, one per line

`Yields:`, `Receives:` and `References:` are recognized as headers, but their
content is dropped. Text before the first header is split at the first blank
line into the summary and the remarks. See [What the pages show](#what-the-pages-show)
for which of these sections are rendered.

### `pythonPath`

Path to the Python executable. Default: tries `python3` first, falls back
to `python`. On Windows, `python3` is often a Microsoft Store alias that
returns exit code 9009. The plugin treats that exit code, exit code 127 and a
missing executable as "not found" and tries the next candidate. When you set
`pythonPath`, only that executable is tried.

Set this to a full path if Python isn't on your PATH:

```yaml
pythonPath: /usr/local/bin/python3.12
```

### Multiple packages

Each `plugins` entry processes one `source` directory. To document several
packages, add one entry per package, each with its own `routePrefix`. Every
entry starts from the default options, so settings from one entry don't carry
over to the next.

```yaml
plugins:
  - name: mokadocs-python-api
    options:
      source: ./packages/core
      label: "Core"
      routePrefix: /core-api
  - name: mokadocs-python-api
    options:
      source: ./packages/cli
      label: "CLI"
      routePrefix: /cli-api
```

---

## What gets extracted

### Types

| Python construct | Mapped to | Detection |
|---|---|---|
| `class Foo:` | Class | Default for class nodes |
| `class Foo(Protocol):` or `class Foo(ABC):` | Interface | A base named `Protocol`, `ABC` or `ABCMeta`, or ending in `Protocol` |
| `@dataclass class Foo:` | Record | Has a `@dataclass` or `@dataclasses.dataclass` decorator |
| `class Color(Enum):` | Enum | A base named `Enum`, `IntEnum`, `StrEnum`, `Flag` or `IntFlag`, or ending in `.Enum` |
| Module-level functions | Static methods on a synthetic "module" type | Functions not inside a class |

The checks run in the order Enum, Interface, Record. Only top-level classes are
extracted; classes nested inside other classes are not. Names starting with `_`
are skipped, except dunder methods such as `__init__` and `__str__`.

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
| `name: str = "default"` | Field |
| Enum values (`RED = "red"`) | Field (static) |

Class variables without a type annotation are skipped, except enum values.
A property setter or deleter (`@name.setter`) shows up as a separate method
with the property's name.

In a class docstring, an `Args:` entry becomes the description of the
annotated class variable with the same name. The `Attributes:` section does
not describe fields; see [What the pages show](#what-the-pages-show).

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
- Exceptions table → ZeroDivisionError: "If b is zero."
- Examples → a Python code block in the method's detail block

### Type annotations

Annotations are kept as written (through `ast.unparse`) and appear in member
signatures and parameter tables:

```python
def greet(name: str, greeting: str = "Hello") -> str:
    """Build a greeting.

    Args:
        name: Who to greet.
        greeting: The greeting word.
    """
```

→ Signature: `def greet(name: str, greeting: str='Hello') -> str`
→ Parameters: `name` (str, "Who to greet."), `greeting` (str, "The greeting word.")

Parameter tables show the name, type and description only. Default values
appear in the signature but not in the table, and `Optional[T]` is not marked
as nullable. `*args` and `**kwargs` are included in the parameter list, with
`params` shown before the type of `*args`. Positional-only parameters (before
`/`) are not listed.

### `__all__` exports

If a module defines `__all__` as a list or tuple of strings, only the listed
classes and functions from that module are included in the generated docs. This
lets you control the public API surface without renaming private members. Each
module's `__all__` only applies to that module, so re-exports in a package's
`__init__.py` don't change what is documented for its submodules.

### Decorators

Decorators appear above the declaration, such as `@dataclass` above a class.
Only the name is kept, so `@lru_cache(maxsize=32)` shows as `@lru_cache`.
`@property`, `@staticmethod`, `@classmethod` and `@abstractmethod` aren't shown
on members, because they set the member kinds and flags listed above.
`@dataclass` also makes a class a Record.

A function or method decorated with a bare `deprecated` or `deprecation` name,
such as `@deprecated` or `@deprecated("Use add")`, gets an **obsolete** badge in
its member table. Dotted forms like `@warnings.deprecated` are not detected, and
classes are never marked.

---

## What the pages show

The plugin reuses the C# page renderer, so Python pages follow the C# layout,
including C#-style declarations.

The index page at `routePrefix` shows the `label` as its title and one table
per module listing each type with its kind and summary. A module's functions
are listed as a type named after the module.

Each type page includes:
- Kind badges (Class / Enum / Record / Interface, plus Static on module
  function pages and Abstract on `ABC` subclasses)
- A C#-style declaration in a `csharp` code block, such as
  `public class Calculator`, `public enum Color : Enum` or
  `public static class calculator`
- A `Namespace:` line with the module name (on a module's functions page,
  the parent package if there is one)
- The summary from the class or module docstring
- Remarks, with line breaks collapsed. Text between `**` pairs is bold,
  which covers the `Note:` labels and `Attributes:` entry names the analyzer
  adds; other Markdown is not converted. `Attributes:` entries only become the
  remarks when the docstring has no other remarks text; otherwise they are
  dropped.
- Inheritance (base classes + protocols)
- Tables of constructors, properties, methods and fields with their summaries.
  Parameter lists in these tables use C# order (`add(float a, float b)`), and
  three or more parameters are shortened to `(…)`. Enum values are listed by
  name, without their values.
- Details for members whose docstring has a summary or at least one `Args:`
  entry, or that have a decorator the page shows: the Python signature (in a
  `csharp` code block) with its decorators, the summary, a parameter table
  when `Args:` documents at least one parameter, the return value, the
  exceptions, remarks, examples and See Also entries
- Examples from the class or module docstring, as a Python code block
- See Also entries, as code text rather than links. An entry written as a
  Markdown link, `[text](https://...)`, becomes a link.
- A Mermaid type relationship diagram, when the type has a base class,
  protocols or subclasses
- A View Source panel with the first lines of the class

---

## Output

The plugin generates:

```
_site/
  {routePrefix}/
    index.html                          ← index listing all modules + types
    {module path}/
      index.html                        ← the module's functions (or a redirect if it has none)
      {typename}/index.html             ← per-type page (same layout as C# API)
```

---

## Requirements

- **Python 3.9 or later** on the build machine (the analyzer uses
  `ast.unparse()`, which was added in Python 3.9). No pip packages required.
- The Python source files must be **syntactically valid**. The analyzer
  skips files that `ast.parse()` rejects and reports each one as a build
  warning, not a build failure.

---

## Limitations

- **Google-style docstrings only** - NumPy-style and reStructuredText
  (Sphinx) formats are not parsed. A plain docstring without section
  headers becomes the summary and remarks.
- **No runtime analysis** - the plugin uses static AST parsing, not
  `import`. Dynamic attributes, monkey-patched methods, and metaclass-
  generated members are not discovered.
- **No cross-reference linking** - type names in annotations, docstrings
  and See Also entries are rendered as plain text, not hyperlinks to
  other type pages.
- **Top-level classes only** - nested classes are not extracted.
- **Docstrings are plain text** - HTML in a docstring is shown as written,
  not rendered.
- **Single source directory** - the plugin processes one `source:`
  directory per declaration. See [Multiple packages](#multiple-packages).

---

## Example

Given this Python source in `src/shapes.py`:

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
      routePrefix: /python
```

The module is `shapes`, and the plugin produces pages at:
- `/python` - index with Color (Enum), DataPoint (Record), Calculator (Class)
- `/python/shapes/color` - enum listing the RED, GREEN and BLUE fields
- `/python/shapes/datapoint` - dataclass with label, value and unit fields, and
  the `Attributes:` section as its remarks
- `/python/shapes/calculator` - class with its constructor and the documented
  add method
