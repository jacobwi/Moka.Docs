#!/usr/bin/env python3
"""
mokadocs Python API analyzer.

Reads Python source files from a directory, parses them via the `ast` module,
extracts classes/functions/docstrings, and outputs JSON matching the mokadocs
ApiModels.cs schema so the C# PythonApiPlugin can generate API reference pages.

Zero external dependencies — uses only the Python standard library.

Usage:
    python mokadocs_python_analyzer.py <source_dir> [--output <path>] [--format google]
"""

import ast
import json
import os
import re
import sys
import textwrap
from dataclasses import dataclass, field, asdict
from pathlib import Path
from typing import Any, Optional


# ── Data models (mirror ApiModels.cs) ─────────────────────────────────────

@dataclass
class DocBlock:
    summary: str = ""
    remarks: str = ""
    parameters: dict[str, str] = field(default_factory=dict)
    typeParameters: dict[str, str] = field(default_factory=dict)
    returns: str = ""
    value: str = ""
    exceptions: list[dict[str, str]] = field(default_factory=list)
    examples: list[str] = field(default_factory=list)
    seeAlso: list[str] = field(default_factory=list)
    isInherited: bool = False


@dataclass
class Parameter:
    name: str
    type: str = ""
    defaultValue: Optional[str] = None
    hasDefaultValue: bool = False
    isParams: bool = False  # *args
    isRef: bool = False
    isOut: bool = False
    isIn: bool = False
    isNullable: bool = False


@dataclass
class Attribute:
    name: str
    arguments: str = ""


@dataclass
class TypeParameter:
    name: str
    constraints: list[str] = field(default_factory=list)
    documentation: str = ""


@dataclass
class Member:
    name: str
    kind: str  # Constructor, Method, Property, Field, Event, Operator, Indexer
    signature: str = ""
    returnType: Optional[str] = None
    accessibility: str = "Public"
    isStatic: bool = False
    isVirtual: bool = False
    isAbstract: bool = False
    isOverride: bool = False
    isSealed: bool = False
    isExtensionMethod: bool = False
    parameters: list[Parameter] = field(default_factory=list)
    typeParameters: list[TypeParameter] = field(default_factory=list)
    documentation: Optional[DocBlock] = None
    attributes: list[Attribute] = field(default_factory=list)
    isObsolete: bool = False
    obsoleteMessage: Optional[str] = None


@dataclass
class ApiType:
    name: str
    fullName: str
    kind: str  # Class, Struct, Record, Interface, Enum, Delegate
    accessibility: str = "Public"
    isStatic: bool = False
    isAbstract: bool = False
    isSealed: bool = False
    isRecord: bool = False
    typeParameters: list[TypeParameter] = field(default_factory=list)
    baseType: Optional[str] = None
    implementedInterfaces: list[str] = field(default_factory=list)
    members: list[Member] = field(default_factory=list)
    documentation: Optional[DocBlock] = None
    attributes: list[Attribute] = field(default_factory=list)
    namespace: Optional[str] = None
    assembly: Optional[str] = None
    sourcePath: Optional[str] = None
    isObsolete: bool = False
    obsoleteMessage: Optional[str] = None
    sourceCode: Optional[str] = None


@dataclass
class Namespace:
    name: str
    types: list[ApiType] = field(default_factory=list)


@dataclass
class AnalysisResult:
    assemblies: list[str] = field(default_factory=list)
    namespaces: list[Namespace] = field(default_factory=list)


# ── Google-style docstring parser ─────────────────────────────────────────

_SECTION_RE = re.compile(
    r"^(Args|Arguments|Parameters|Params|Returns|Return|Raises|Throws|"
    r"Examples?|Attributes|Note|Notes|Warning|Warnings|"
    r"Todo|Todos|See Also|References|Yields?|Receives?)\s*:\s*$",
    re.IGNORECASE,
)


def parse_google_docstring(docstring: str | None) -> DocBlock:
    """Parse a Google-style docstring into a structured DocBlock."""
    if not docstring:
        return DocBlock()

    doc = textwrap.dedent(docstring).strip()
    lines = doc.splitlines()

    summary_lines: list[str] = []
    sections: dict[str, list[str]] = {}
    current_section: str | None = None

    for line in lines:
        m = _SECTION_RE.match(line.strip())
        if m:
            current_section = m.group(1).lower().rstrip("s")  # normalize plural
            if current_section == "argument" or current_section == "param" or current_section == "parameter":
                current_section = "arg"
            elif current_section == "return":
                current_section = "return"
            elif current_section == "raise" or current_section == "throw":
                current_section = "raise"
            elif current_section == "attribute":
                current_section = "attribute"
            sections.setdefault(current_section, [])
            continue

        if current_section is not None:
            sections.setdefault(current_section, []).append(line)
        else:
            summary_lines.append(line)

    # Split summary from remarks (first blank line separates them)
    summary = ""
    remarks = ""
    if summary_lines:
        parts = "\n".join(summary_lines).split("\n\n", 1)
        summary = parts[0].strip()
        if len(parts) > 1:
            remarks = parts[1].strip()

    # Parse Args section into parameter docs
    parameters: dict[str, str] = {}
    if "arg" in sections:
        parameters = _parse_arg_section(sections["arg"])

    # Parse Returns
    returns = "\n".join(sections.get("return", [])).strip()

    # Parse Raises
    exceptions: list[dict[str, str]] = []
    if "raise" in sections:
        for exc_name, exc_desc in _parse_arg_section(sections["raise"]).items():
            exceptions.append({"type": exc_name, "description": exc_desc})

    # Parse Examples
    examples: list[str] = []
    if "example" in sections:
        example_text = "\n".join(sections["example"]).strip()
        if example_text:
            examples.append(example_text)

    # Parse Attributes (for dataclasses)
    attributes_doc: dict[str, str] = {}
    if "attribute" in sections:
        attributes_doc = _parse_arg_section(sections["attribute"])
        # Merge attribute docs into remarks if useful
        if attributes_doc and not remarks:
            attr_parts = [f"**{k}**: {v}" for k, v in attributes_doc.items()]
            remarks = "Attributes:\n" + "\n".join(attr_parts)

    # Parse Notes/Warnings into remarks
    for section_key in ("note", "warning", "todo"):
        if section_key in sections:
            note_text = "\n".join(sections[section_key]).strip()
            if note_text:
                label = section_key.capitalize()
                if remarks:
                    remarks += f"\n\n**{label}:** {note_text}"
                else:
                    remarks = f"**{label}:** {note_text}"

    # See Also
    see_also: list[str] = []
    if "see also" in sections:
        for line in sections["see also"]:
            stripped = line.strip()
            if stripped:
                see_also.append(stripped)

    return DocBlock(
        summary=summary,
        remarks=remarks,
        parameters=parameters,
        returns=returns,
        exceptions=exceptions,
        examples=examples,
        seeAlso=see_also,
    )


def _parse_arg_section(lines: list[str]) -> dict[str, str]:
    """Parse an indented Args-style section into name → description pairs.

    Handles:
        name (type): Description that may
            span multiple lines.
        name: Description without type.
    """
    result: dict[str, str] = {}
    current_name: str | None = None
    current_desc: list[str] = []

    for line in lines:
        stripped = line.strip()
        if not stripped:
            if current_name:
                current_desc.append("")
            continue

        # Check if this line starts a new parameter (indented with name)
        m = re.match(r"^(\w+)\s*(?:\(.*?\))?\s*:\s*(.*)", stripped)
        if m and not line.startswith("        "):  # not a continuation
            if current_name:
                result[current_name] = " ".join(current_desc).strip()
            current_name = m.group(1)
            current_desc = [m.group(2)] if m.group(2) else []
        elif current_name:
            current_desc.append(stripped)

    if current_name:
        result[current_name] = " ".join(current_desc).strip()

    return result


# ── AST analysis ──────────────────────────────────────────────────────────

def _get_annotation_str(node: ast.expr | None) -> str:
    """Convert an AST annotation node to a readable type string."""
    if node is None:
        return ""
    return ast.unparse(node)


def _get_decorator_names(decorators: list[ast.expr]) -> list[str]:
    """Extract decorator names from a list of decorator nodes."""
    names: list[str] = []
    for dec in decorators:
        if isinstance(dec, ast.Name):
            names.append(dec.id)
        elif isinstance(dec, ast.Attribute):
            names.append(ast.unparse(dec))
        elif isinstance(dec, ast.Call):
            if isinstance(dec.func, ast.Name):
                names.append(dec.func.id)
            elif isinstance(dec.func, ast.Attribute):
                names.append(ast.unparse(dec.func))
    return names


def _detect_type_kind(node: ast.ClassDef) -> str:
    """Determine the ApiTypeKind from a class definition."""
    decorators = _get_decorator_names(node.decorator_list)

    # Enum detection (base class is Enum, IntEnum, StrEnum, Flag, etc.)
    enum_bases = {"Enum", "IntEnum", "StrEnum", "Flag", "IntFlag", "auto"}
    for base in node.bases:
        base_name = ast.unparse(base)
        if base_name in enum_bases or base_name.endswith(".Enum"):
            return "Enum"

    # Protocol / ABC detection → Interface
    interface_bases = {"Protocol", "ABC", "ABCMeta"}
    for base in node.bases:
        base_name = ast.unparse(base)
        if base_name in interface_bases or base_name.endswith("Protocol"):
            return "Interface"

    # Dataclass detection → Record
    if "dataclass" in decorators or "dataclasses.dataclass" in decorators:
        return "Record"

    return "Class"


def _is_public(name: str) -> bool:
    """Check if a name is public (doesn't start with underscore, except __init__ etc.)."""
    if name.startswith("__") and name.endswith("__"):
        return True  # dunder methods are public
    return not name.startswith("_")


def _build_function_signature(node: ast.FunctionDef | ast.AsyncFunctionDef) -> str:
    """Build a human-readable function signature string."""
    prefix = "async " if isinstance(node, ast.AsyncFunctionDef) else ""
    params = ast.unparse(node.args) if node.args.args or node.args.vararg or node.args.kwarg else ""
    ret = f" -> {ast.unparse(node.returns)}" if node.returns else ""
    return f"{prefix}def {node.name}({params}){ret}"


def _extract_parameters(node: ast.FunctionDef | ast.AsyncFunctionDef) -> list[Parameter]:
    """Extract Parameter objects from a function's arguments."""
    params: list[Parameter] = []
    args = node.args

    # Calculate default offset (defaults align to the END of the args list)
    num_args = len(args.args)
    num_defaults = len(args.defaults)
    default_offset = num_args - num_defaults

    for i, arg in enumerate(args.args):
        if arg.arg in ("self", "cls"):
            continue
        p = Parameter(
            name=arg.arg,
            type=_get_annotation_str(arg.annotation),
        )
        default_idx = i - default_offset
        if default_idx >= 0 and default_idx < len(args.defaults):
            p.hasDefaultValue = True
            try:
                p.defaultValue = ast.unparse(args.defaults[default_idx])
            except Exception:
                p.defaultValue = "..."

        if p.type and p.type.startswith("Optional["):
            p.isNullable = True

        params.append(p)

    # *args
    if args.vararg:
        params.append(Parameter(
            name=args.vararg.arg,
            type=_get_annotation_str(args.vararg.annotation),
            isParams=True,
        ))

    # keyword-only args
    for i, arg in enumerate(args.kwonlyargs):
        p = Parameter(
            name=arg.arg,
            type=_get_annotation_str(arg.annotation),
        )
        if i < len(args.kw_defaults) and args.kw_defaults[i] is not None:
            p.hasDefaultValue = True
            try:
                p.defaultValue = ast.unparse(args.kw_defaults[i])
            except Exception:
                p.defaultValue = "..."
        params.append(p)

    # **kwargs
    if args.kwarg:
        params.append(Parameter(
            name=args.kwarg.arg,
            type=_get_annotation_str(args.kwarg.annotation),
        ))

    return params


def analyze_class(node: ast.ClassDef, module_name: str, source_lines: list[str]) -> ApiType:
    """Analyze a class definition and return an ApiType."""
    kind = _detect_type_kind(node)
    decorators = _get_decorator_names(node.decorator_list)

    # Base types
    bases = [ast.unparse(b) for b in node.bases]
    base_type = bases[0] if bases else None
    interfaces = bases[1:] if len(bases) > 1 else []

    # For protocols/ABCs, all bases are "interfaces"
    if kind == "Interface":
        interfaces = bases
        base_type = None

    # Docstring
    docstring = ast.get_docstring(node)
    doc = parse_google_docstring(docstring)

    # Attributes (decorators)
    attrs = [Attribute(name=d, arguments="") for d in decorators]

    # Source code snippet (first few lines)
    source_code = ""
    if node.end_lineno:
        src_lines = source_lines[node.lineno - 1:min(node.end_lineno, node.lineno + 20)]
        source_code = "\n".join(src_lines)

    # Is abstract?
    is_abstract = "abstractmethod" in decorators or "ABC" in [ast.unparse(b) for b in node.bases]

    # Members
    members: list[Member] = []
    for item in ast.iter_child_nodes(node):
        if isinstance(item, (ast.FunctionDef, ast.AsyncFunctionDef)):
            member = _analyze_method(item, decorators_context=decorators)
            if member:
                members.append(member)
        elif isinstance(item, ast.AnnAssign) and isinstance(item.target, ast.Name):
            # Class variable with annotation: name: type [= value]
            name = item.target.id
            if _is_public(name):
                member_doc = DocBlock()
                if doc.parameters and name in doc.parameters:
                    member_doc.summary = doc.parameters[name]
                m = Member(
                    name=name,
                    kind="Field",
                    signature=f"{name}: {_get_annotation_str(item.annotation)}",
                    returnType=_get_annotation_str(item.annotation),
                    documentation=member_doc if member_doc.summary else None,
                )
                if item.value is not None:
                    try:
                        m.signature += f" = {ast.unparse(item.value)}"
                    except Exception:
                        pass
                members.append(m)

    # For enums, extract enum values as fields
    if kind == "Enum":
        for item in ast.iter_child_nodes(node):
            if isinstance(item, ast.Assign):
                for target in item.targets:
                    if isinstance(target, ast.Name) and _is_public(target.id):
                        try:
                            val = ast.unparse(item.value)
                        except Exception:
                            val = "..."
                        members.append(Member(
                            name=target.id,
                            kind="Field",
                            signature=f"{target.id} = {val}",
                            returnType=node.name,
                            isStatic=True,
                        ))

    return ApiType(
        name=node.name,
        fullName=f"{module_name}.{node.name}",
        kind=kind,
        isAbstract=is_abstract,
        isRecord=kind == "Record",
        baseType=base_type,
        implementedInterfaces=interfaces,
        members=members,
        documentation=doc if doc.summary else None,
        attributes=attrs,
        namespace=module_name,
        sourceCode=source_code,
    )


def _analyze_method(
    node: ast.FunctionDef | ast.AsyncFunctionDef,
    decorators_context: list[str] | None = None,
) -> Member | None:
    """Analyze a method/function definition."""
    name = node.name
    decorators = _get_decorator_names(node.decorator_list)

    # Skip private methods (but keep dunders like __init__, __str__, etc.)
    if not _is_public(name):
        return None

    # Determine member kind
    kind = "Method"
    is_static = False
    is_abstract = False

    if name == "__init__":
        kind = "Constructor"
    elif "property" in decorators:
        kind = "Property"
    elif "staticmethod" in decorators:
        is_static = True
    elif "classmethod" in decorators:
        is_static = True
    if "abstractmethod" in decorators:
        is_abstract = True

    # Build signature
    signature = _build_function_signature(node)

    # Return type
    return_type = _get_annotation_str(node.returns) if node.returns else None

    # Parameters
    parameters = _extract_parameters(node)

    # Docstring
    docstring = ast.get_docstring(node)
    doc = parse_google_docstring(docstring)

    # Merge parameter docs from docstring into parameter objects
    for p in parameters:
        if p.name in doc.parameters:
            pass  # doc.parameters already has the description

    # Attributes (decorators)
    attrs = [Attribute(name=d, arguments="") for d in decorators if d not in ("property", "staticmethod", "classmethod", "abstractmethod")]

    # Obsolete detection
    is_obsolete = "deprecated" in decorators or "deprecation" in decorators
    obsolete_msg = None
    if is_obsolete:
        for dec in node.decorator_list:
            if isinstance(dec, ast.Call) and hasattr(dec, "args") and dec.args:
                try:
                    obsolete_msg = ast.unparse(dec.args[0])
                except Exception:
                    pass

    return Member(
        name=name,
        kind=kind,
        signature=signature,
        returnType=return_type,
        isStatic=is_static,
        isAbstract=is_abstract,
        parameters=parameters,
        documentation=doc if doc.summary or doc.parameters else None,
        attributes=attrs,
        isObsolete=is_obsolete,
        obsoleteMessage=obsolete_msg,
    )


def analyze_module_functions(
    tree: ast.Module, module_name: str, source_lines: list[str]
) -> ApiType | None:
    """Extract module-level functions into a synthetic 'module' type."""
    functions: list[Member] = []

    for node in ast.iter_child_nodes(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            if _is_public(node.name):
                member = _analyze_method(node)
                if member:
                    member.isStatic = True  # Module-level = static
                    functions.append(member)

    if not functions:
        return None

    # Module docstring
    docstring = ast.get_docstring(tree)
    doc = parse_google_docstring(docstring)

    # Synthetic type representing the module itself
    short_name = module_name.rsplit(".", 1)[-1]
    return ApiType(
        name=short_name,
        fullName=module_name,
        kind="Class",
        isStatic=True,
        members=functions,
        documentation=doc if doc.summary else None,
        namespace=module_name.rsplit(".", 1)[0] if "." in module_name else module_name,
        attributes=[Attribute(name="module", arguments="")],
    )


# ── File discovery + orchestration ────────────────────────────────────────

def discover_python_files(
    source_dir: str,
    include: list[str] | None = None,
    exclude: list[str] | None = None,
) -> list[Path]:
    """Discover .py files in the source directory."""
    root = Path(source_dir).resolve()
    if not root.is_dir():
        print(f"Error: source directory does not exist: {source_dir}", file=sys.stderr)
        sys.exit(1)

    default_exclude = {"__pycache__", ".git", ".venv", "venv", "node_modules", ".tox", ".mypy_cache"}

    files: list[Path] = []
    for py_file in root.rglob("*.py"):
        # Skip excluded directories
        parts = py_file.relative_to(root).parts
        if any(p in default_exclude for p in parts):
            continue
        if any(p.startswith(".") for p in parts):
            continue

        # Skip test files by default
        name = py_file.name
        if name.startswith("test_") or name.endswith("_test.py"):
            continue
        if name in ("setup.py", "conftest.py", "noxfile.py", "fabfile.py"):
            continue

        files.append(py_file)

    return sorted(files)


def file_to_module_name(source_dir: Path, py_file: Path) -> str:
    """Convert a file path to a Python module name (dot-separated)."""
    rel = py_file.relative_to(source_dir)
    parts = list(rel.parts)
    if parts[-1] == "__init__.py":
        parts = parts[:-1]
    else:
        parts[-1] = parts[-1].removesuffix(".py")
    return ".".join(parts) if parts else rel.stem


def analyze_directory(source_dir: str) -> AnalysisResult:
    """Analyze all Python files in a directory and return an AnalysisResult."""
    root = Path(source_dir).resolve()
    files = discover_python_files(source_dir)

    # Group by top-level package
    package_name = root.name
    namespaces: dict[str, Namespace] = {}

    for py_file in files:
        module_name = file_to_module_name(root, py_file)
        if not module_name:
            continue

        try:
            source = py_file.read_text(encoding="utf-8", errors="replace")
            source_lines = source.splitlines()
            tree = ast.parse(source, filename=str(py_file))
        except SyntaxError as e:
            print(f"Warning: syntax error in {py_file}: {e}", file=sys.stderr)
            continue

        # Check __all__ for explicit exports
        all_exports: set[str] | None = None
        for node in ast.iter_child_nodes(tree):
            if isinstance(node, ast.Assign):
                for target in node.targets:
                    if isinstance(target, ast.Name) and target.id == "__all__":
                        if isinstance(node.value, (ast.List, ast.Tuple)):
                            all_exports = set()
                            for elt in node.value.elts:
                                if isinstance(elt, ast.Constant) and isinstance(elt.value, str):
                                    all_exports.add(elt.value)

        # Determine namespace (module path without the type name)
        ns_name = module_name

        # Extract classes
        for node in ast.iter_child_nodes(tree):
            if isinstance(node, ast.ClassDef):
                if all_exports is not None and node.name not in all_exports:
                    continue
                if not _is_public(node.name):
                    continue

                api_type = analyze_class(node, module_name, source_lines)
                api_type.sourcePath = str(py_file.relative_to(root))

                ns = namespaces.setdefault(ns_name, Namespace(name=ns_name))
                ns.types.append(api_type)

        # Extract module-level functions
        module_type = analyze_module_functions(tree, module_name, source_lines)
        if module_type:
            if all_exports is not None:
                module_type.members = [
                    m for m in module_type.members
                    if m.name in all_exports
                ]
            if module_type.members:
                module_type.sourcePath = str(py_file.relative_to(root))
                ns = namespaces.setdefault(ns_name, Namespace(name=ns_name))
                ns.types.append(module_type)

    return AnalysisResult(
        assemblies=[package_name],
        namespaces=list(namespaces.values()),
    )


# ── JSON serialization ────────────────────────────────────────────────────

def _clean_dict(d: Any) -> Any:
    """Recursively remove None values and empty collections from dicts for cleaner JSON."""
    if isinstance(d, dict):
        return {k: _clean_dict(v) for k, v in d.items() if v is not None and v != "" and v != [] and v != {}}
    elif isinstance(d, list):
        return [_clean_dict(item) for item in d]
    return d


def to_json(result: AnalysisResult) -> str:
    """Serialize an AnalysisResult to JSON matching the ApiModels.cs schema."""
    raw = asdict(result)
    cleaned = _clean_dict(raw)
    return json.dumps(cleaned, indent=2, ensure_ascii=False)


# ── CLI entry point ───────────────────────────────────────────────────────

def main() -> None:
    import argparse

    parser = argparse.ArgumentParser(
        description="Analyze Python source files and output API metadata as JSON."
    )
    parser.add_argument("source_dir", help="Path to the Python source directory")
    parser.add_argument("--output", "-o", help="Output JSON file path (default: stdout)")
    parser.add_argument("--format", default="google", choices=["google"],
                        help="Docstring format (default: google)")

    args = parser.parse_args()

    result = analyze_directory(args.source_dir)
    json_output = to_json(result)

    if args.output:
        Path(args.output).write_text(json_output, encoding="utf-8")
        print(f"Wrote API analysis to {args.output}", file=sys.stderr)
    else:
        print(json_output)


if __name__ == "__main__":
    main()
