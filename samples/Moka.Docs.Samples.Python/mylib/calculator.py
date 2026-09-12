"""Calculator module with basic arithmetic operations."""

from dataclasses import dataclass
from enum import Enum
from typing import Optional


class Color(Enum):
    """An enumeration of supported colors.

    Examples:
        >>> Color.RED.value
        'red'
    """

    RED = "red"
    GREEN = "green"
    BLUE = "blue"


@dataclass
class DataPoint:
    """A single data point with a label and value.

    Attributes:
        label: Human-readable label for the data point.
        value: Numeric value.
        unit: Optional unit of measurement.
    """

    label: str
    value: float
    unit: Optional[str] = None


class Calculator:
    """A simple calculator supporting basic arithmetic.

    This class demonstrates how mokadocs generates API docs for Python classes
    including constructors, methods, properties, and docstrings.

    Args:
        precision: Number of decimal places for results.

    Examples:
        >>> calc = Calculator(precision=2)
        >>> calc.add(1.1, 2.2)
        3.3
    """

    def __init__(self, precision: int = 2) -> None:
        """Initialize the calculator.

        Args:
            precision: Number of decimal places to round results to.
        """
        self._precision = precision
        self._history: list[float] = []

    @property
    def precision(self) -> int:
        """The current rounding precision."""
        return self._precision

    @property
    def history(self) -> list[float]:
        """List of all computed results."""
        return self._history.copy()

    def add(self, a: float, b: float) -> float:
        """Add two numbers.

        Args:
            a: First operand.
            b: Second operand.

        Returns:
            The sum of a and b, rounded to the configured precision.

        Examples:
            >>> Calculator().add(1, 2)
            3.0
        """
        result = round(a + b, self._precision)
        self._history.append(result)
        return result

    def divide(self, a: float, b: float) -> float:
        """Divide a by b.

        Args:
            a: Dividend.
            b: Divisor.

        Returns:
            The quotient of a and b.

        Raises:
            ZeroDivisionError: If b is zero.
        """
        if b == 0:
            raise ZeroDivisionError("Cannot divide by zero")
        result = round(a / b, self._precision)
        self._history.append(result)
        return result

    def clear_history(self) -> None:
        """Clear the computation history."""
        self._history.clear()


def greet(name: str, greeting: str = "Hello") -> str:
    """Generate a greeting message.

    Args:
        name: The person's name.
        greeting: The greeting word to use.

    Returns:
        A formatted greeting string.

    Examples:
        >>> greet("World")
        'Hello, World!'
    """
    return f"{greeting}, {name}!"
