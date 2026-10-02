using System.Globalization;

namespace Encina.Security.Sanitization.Sanitizers;

/// <summary>
/// Provides defense-in-depth sanitization for SQL contexts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> Parameterized queries are always the preferred defense against
/// SQL injection. This sanitizer provides an additional layer of protection for scenarios
/// where parameterization is not possible (e.g., dynamic column names, ORDER BY clauses).
/// </para>
/// <para>
/// Quotes are escaped once, then the input is scanned in a single pass that appends each
/// character to the output and removes any dangerous token the output now ends with
/// (<c>--</c>, <c>;</c>, <c>/*</c>, <c>*/</c>, <c>xp_</c> and the word characters after it).
/// Because removal happens at the output tail as characters arrive, a removal that joins its
/// neighbours into a new token is reduced again immediately, so no marker can be returned.
/// A <c>/* ... */</c> block is removed with its content: once a <c>/*</c> opens, the raw content
/// is kept until the first <c>*/</c> closes it; inside the open comment <c>;</c>, <c>--</c> and
/// <c>xp_</c> are still reduced, so <c>/*x*;/y</c> gives <c>y</c>. A <c>/*</c> that never closes loses only
/// the marker; its content is scanned once more in plain mode. Every character is appended a
/// bounded number of times and removed at most once, so the work is linear in the input length.
/// </para>
/// </remarks>
internal static class SqlSanitizer
{
    /// <summary>
    /// Sanitizes input for safe use in SQL contexts.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>The sanitized string with SQL-dangerous patterns neutralized.</returns>
    internal static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Escape single quotes (SQL string delimiter): ' → ''. Runs once, never inside the scan.
        var escaped = input.Replace("'", "''", StringComparison.Ordinal);

        var scanner = new TailScanner(escaped.Length);
        foreach (var c in escaped)
        {
            scanner.Append(c);
        }

        return scanner.Finish();
    }

    /// <summary>
    /// Builds the output one character at a time, reducing the output tail after every append.
    /// </summary>
    private sealed class TailScanner
    {
        private readonly char[] _buffer;
        private int _length;

        // Output length at which the content of the open "/*" begins; -1 when no comment is open.
        private int _commentStart = -1;

        // Output length right after the last removed "xp_": while the output is back at that
        // length, the word characters that follow belong to the removed match and are dropped.
        // -1 when there is none.
        private int _swallowAt = -1;

        // True while the content of an unclosed comment is re-scanned: "/*" and "*/" are plain tokens.
        private bool _plain;

        internal TailScanner(int capacity) => _buffer = new char[capacity];

        internal void Append(char c)
        {
            if (_length == _swallowAt && IsWordChar(c))
            {
                return;
            }

            _buffer[_length++] = c;
            ReduceAll();
            if (_commentStart >= 0)
            {
                CloseCommentIfComplete();
            }
        }

        internal string Finish()
        {
            if (_commentStart >= 0)
            {
                RescanUnclosedComment();
            }

            return new string(_buffer, 0, _length);
        }

        // Inside an open comment only ";", "--" and "xp_" are reduced; "/*" and "*/" are content
        // until the first "*/" closes the comment and drops everything since its marker.
        private void CloseCommentIfComplete()
        {
            if (_length - 2 >= _commentStart && EndsWith('*', '/'))
            {
                Truncate(_commentStart);
                _commentStart = -1;
                ReduceAll();
            }
        }

        // The comment never closed: only its marker was removed, so the content is scanned again.
        private void RescanUnclosedComment()
        {
            var content = new char[_length - _commentStart];
            Array.Copy(_buffer, _commentStart, content, 0, content.Length);
            Truncate(_commentStart);
            _commentStart = -1;
            _plain = true;
            foreach (var c in content)
            {
                Append(c);
            }
        }

        private void ReduceAll()
        {
            while (ReduceTail())
            {
            }
        }

        // Removes one dangerous token from the end of the output; false when there is none.
        // The tokens end in different characters, so the order of the checks does not matter.
        private bool ReduceTail()
            => ReduceStatementToken()
            || (_commentStart < 0 && ReduceCommentToken())
            || (EndsWithExtendedProc() && DropExtendedProc());

        private bool ReduceStatementToken()
        {
            if (EndsWith(';'))
            {
                return Drop(1);
            }

            return EndsWith('-', '-') && Drop(2);
        }

        private bool ReduceCommentToken()
        {
            if (EndsWith('*', '/'))
            {
                return Drop(2);
            }

            return EndsWith('/', '*') && OpenComment();
        }

        private bool EndsWith(char last) => _length >= 1 && _buffer[_length - 1] == last;

        private bool EndsWith(char first, char last)
            => _length >= 2 && _buffer[_length - 2] == first && _buffer[_length - 1] == last;

        private bool EndsWithExtendedProc()
            => _length >= 3
            && char.ToUpperInvariant(_buffer[_length - 3]) == 'X'
            && char.ToUpperInvariant(_buffer[_length - 2]) == 'P'
            && _buffer[_length - 1] == '_';

        private bool Drop(int count)
        {
            Truncate(_length - count);
            return true;
        }

        private void Truncate(int newLength)
        {
            _length = newLength;
            if (_commentStart > _length)
            {
                _commentStart = _length;
            }

            if (_swallowAt > _length)
            {
                _swallowAt = -1;
            }
        }

        private bool DropExtendedProc()
        {
            Drop(3);
            _swallowAt = _length;
            return true;
        }

        // "/*": the marker is removed; outside plain mode it opens a block comment.
        private bool OpenComment()
        {
            Drop(2);
            if (!_plain)
            {
                _commentStart = _length;
            }

            return true;
        }

        // Equivalent of the regex \w: letters, marks, decimal digits and connector punctuation.
        private static bool IsWordChar(char c) => CharUnicodeInfo.GetUnicodeCategory(c) is
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation;
    }
}
