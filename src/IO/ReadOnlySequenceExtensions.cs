namespace Rope.IO;

using System.Buffers;

public static class ReadOnlySequenceExtensions
{
	public static SequencePosition? IndexOfAny(this in ReadOnlySequence<byte> sequence, ReadOnlySpan<byte> values)
	{
		var offset = 0;
		foreach (var segment in sequence)
		{
			var index = segment.Span.IndexOfAny(values);
			if (index != -1)
			{
				return sequence.GetPosition(offset + index);
			}

			offset += segment.Length;
		}

		return null;
	}

}
