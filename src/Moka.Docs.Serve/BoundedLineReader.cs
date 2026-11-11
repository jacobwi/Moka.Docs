using System.Text;

namespace Moka.Docs.Serve;

/// <summary>
///     Reads lines of at most a given length. <see cref="TextReader.ReadLineAsync()" /> has no limit,
///     so a worker that wrote one endless line grew the parent's memory without bound.
/// </summary>
/// <remarks>
///     Lines end at <c>\n</c>, and a <c>\r</c> before it is dropped. Not thread-safe: one read at a
///     time.
/// </remarks>
internal sealed class BoundedLineReader(TextReader reader, int maxLength)
{
	private readonly char[] _buffer = new char[8192];
	private readonly StringBuilder _line = new();
	private int _end;
	private int _start;

	/// <summary>
	///     The next line, <c>null</c> at the end of the input, or a too-long result once a line passes
	///     the limit. Nothing more should be read after that.
	/// </summary>
	public async Task<(string? Line, bool TooLong)> ReadLineAsync()
	{
		while (true)
		{
			if (_start < _end)
			{
				int newline = Array.IndexOf(_buffer, '\n', _start, _end - _start);
				int stop = newline >= 0 ? newline : _end;

				// One more than the limit leaves room for the \r of a \r\n.
				if (_line.Length + (stop - _start) > maxLength + 1)
				{
					return (null, true);
				}

				_line.Append(_buffer, _start, stop - _start);
				_start = newline >= 0 ? newline + 1 : _end;

				if (newline >= 0)
				{
					return TakeLine();
				}
			}

			int read = await reader.ReadAsync(_buffer.AsMemory());
			if (read == 0)
			{
				return _line.Length > 0 ? TakeLine() : (null, false);
			}

			_start = 0;
			_end = read;
		}
	}

	private (string? Line, bool TooLong) TakeLine()
	{
		if (_line.Length > 0 && _line[^1] == '\r')
		{
			_line.Length--;
		}

		if (_line.Length > maxLength)
		{
			return (null, true);
		}

		string line = _line.ToString();
		_line.Clear();
		return (line, false);
	}
}
