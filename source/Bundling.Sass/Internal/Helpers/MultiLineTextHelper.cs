using System;
using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling.Sass.Internal.Helpers
{
    internal readonly struct MultiLineTextHelper
    {
        private readonly List<int> _lineIndices;

        private static List<int> GetLineIndices(string text)
        {
            var lineIndices = new List<int>();

            for (int index = 0, n = text.Length; index < n;)
            {
                char c = text[index++];

                if (c == '\r' || c == '\n')
                {
                    if (index < n && (c == '\r' && text[index] == '\n' || c == '\n' && text[index] == '\r'))
                        index++;

                    lineIndices.Add(index);
                }
            }

            return lineIndices;
        }

        public MultiLineTextHelper(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _lineIndices = GetLineIndices(text);
        }

        public bool HasValue => Text != null;

        public string Text { get; }

        public (int LineNumber, int ColumnNumber) MapToPosition(int index)
        {
            if (index == 0)
                return (0, 0);

            if (index < 0 || Text == null || index > Text.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            int lineNumber = _lineIndices.BinarySearch(index);
            if (lineNumber >= 0)
                return (lineNumber + 1, 0);

            lineNumber = ~lineNumber;
            return (lineNumber, lineNumber > 0 ? index - _lineIndices[lineNumber - 1] : index);
        }
    }
}
