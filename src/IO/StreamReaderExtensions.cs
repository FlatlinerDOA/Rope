namespace Rope.IO;

using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Rope;

public static class StreamReaderExtensions
{
	private const byte Quote = 34;
	private const byte Comma = 44;
	private const byte Lf = 10;
	private const byte Cr = 13;
	private static readonly byte[] Delims = [Comma, Lf, Quote, Cr];

    public static object PipeReader { get; private set; }

    /// <summary>
    /// Sets up a pipeline with a function to start writing to the pipeline.
    /// </summary>
    /// <param name="startAsync">Function that starts the processing of data items by writing the channel.</param>
    /// <typeparam name="TOut">The items to be worked on in the pipeline.</typeparam>
    /// <returns>A channel reader that can then be chained.</returns>
    public static ChannelReader<TOut> StartPipelineAsync<TOut>(this Func<ChannelWriter<TOut>, ValueTask> startAsync)
	{
		var channel = Channel.CreateUnbounded<TOut>();
		var writer = channel.Writer;
		var task = Task.Factory.StartNew(
			async _ =>
			{
				await startAsync(writer);
			},
			null,
			CancellationToken.None,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
		task.Unwrap().ContinueWith(t => writer.TryComplete(t.Exception));
		return channel.Reader;
	}

	/// <summary>
	/// Processes items in parallel from a channel reader.
	/// </summary>
	/// <param name="reader">The reader to read items to be processed from.</param>
	/// <param name="selectAsync">The function to do some asynchronous work on.</param>
	/// <param name="maxParallelism">The maximum number of items work on in parallel.</param>
	/// <param name="token">Cancellation token to abort the pipeline (optional)</param>
	/// <typeparam name="TIn">The input item type.</typeparam>
	/// <typeparam name="TOut">The output item type.</typeparam>
	/// <returns>The channel reader to read the processed items from.</returns>
	public static ChannelReader<TOut> ChainAsync<TIn, TOut>(this ChannelReader<TIn> reader, Func<TIn, ValueTask<TOut>> selectAsync, int maxParallelism = 1, CancellationToken token = default)
	{
		var channel = Channel.CreateUnbounded<TOut>();
		var writer = channel.Writer;

		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = maxParallelism,
			CancellationToken = token
		};
		var task = Parallel.ForEachAsync(
			reader.ReadAllAsync(token),
			options,
			async (item, c) =>
			{
				try
				{
					var result = await selectAsync(item);
					await writer.WriteAsync(result, c);
				}
				catch (Exception)
				{
					// Handle the exception and keep processing messages
				}
			});
		task.ContinueWith(t => writer.TryComplete(t.Exception));
		return channel.Reader;
	}

    public static void Seek(this TextReader reader, long offset)
    {
        if (reader is StreamReader sr && sr.BaseStream.CanSeek)
        {
            sr.BaseStream.Seek(offset, SeekOrigin.Begin);
            sr.DiscardBufferedData();
        }
        else
        {
            const int BufferSize = 4096;
            var temp = ArrayPool<char>.Shared.Rent(BufferSize);
            try
            {
                long remaining = offset;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(remaining, BufferSize);
                    int read = reader.ReadBlock(temp.AsSpan(0, toRead));
                    if (read == 0) break; // End of stream
                    remaining -= read;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(temp);
            }
        }
    }

// 	public static IAsyncEnumerable<(Rope<string> Cells, long StartOffset, long EndOffset)> ReadCsvFileAsync(string filePath) // , [EnumeratorCancellation] CancellationToken cancellation
// 	{
// 		var read = new Func<ChannelWriter<(Rope<string> Cells, long StartOffset, long EndOffset)>, ValueTask>(async (writer) =>
// 		{
// 			using var stream = File.OpenRead(filePath);
// 			var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: 64 * 1024));
// 			await ParseCsvFromPipeAsync(reader, writer);
// 		});
		
// 		return read.StartPipelineAsync().ReadAllAsync();
// 	}

// 	public static async Task ParseCsvFromPipeAsync(PEReader reader, ChannelWriter<(Rope<string> Cells, long StartOffset, long EndOffset)> writer) // , [EnumeratorCancellation] CancellationToken cancellation
// 	{
// 		while (true)
// 		{
// 			var result = await reader.ReadAsync().ConfigureAwait(false);
// 			if (result.IsCanceled)
// 			{
// 				break; // exit if we've read everything from the pipe
// 			}

// 			var buffer = result.Buffer;
// 			long totalBytesConsumed = 0;
// 			long startOffset = 0;
// 			while (TryReadCsvLine(buffer, result.IsCompleted, out var cells, out var lineEnds, out var bytesConsumed))
// 			{
// 				var endOffset = buffer.GetOffset(bytesConsumed);
// 				await writer.WriteAsync((cells, startOffset, endOffset)).ConfigureAwait(false);
// 				totalBytesConsumed += endOffset;
// 				startOffset = totalBytesConsumed;
// 				reader.AdvanceTo(bytesConsumed);
// 			}

// 			if (result.IsCompleted)
// 			{
// 				break;
// 			}
// //			var position = ReadCsvLine(buffer, result.IsCompleted, out var partial, out var lineEnds, out var bytesRead); // read complete items from the current buffer
// //			totalBytesConsumed += bytesRead;
// //			cells += partial;
// //			if (lineEnds)
// //			{
// //				if (!cells.IsEmpty)
// //				{
// //					long endOffset = totalBytesConsumed;
// //					yield return (cells, startOffset, endOffset);
// //					startOffset = endOffset;
// //					cells = Rope<string>.Empty;
// //				}
// //			}
// //
// //
// //			pipeReader.AdvanceTo(position, buffer.End); //advance our position in the pipe
// 		}

// 		reader.Complete(); // mark the PipeReader as complete	
// 	}


	public static bool TryReadCsvLine(in ReadOnlySequence<byte> buffer, bool isLastBuffer, out Rope<string> cells, out bool lineEnd, out SequencePosition bytesConsumed)
	{
		cells = Rope<string>.Empty;
		bytesConsumed = buffer.Start;
		lineEnd = false;

		var cell = Rope<byte>.Empty;
		bool isInQuote = false;
		var nextDelim = buffer.IndexOfAny(Delims);
		if (nextDelim.HasValue) // we have a delimiter to inspect
		{
			var delim = buffer.Slice(nextDelim.Value, 1).FirstSpan[0];
			bytesConsumed = buffer.GetPosition(buffer.GetOffset(nextDelim.Value) + 1);
			switch (delim)
			{
				case Comma:
					if (isInQuote)
					{
						cell += delim;
					}
					else
					{
						cell += buffer.Slice(0, nextDelim.Value).ToRope();
						cells += cell.ToString();
						cell = Rope<byte>.Empty;
					}

					break;
				case Cr:
				case Lf:
					if (isInQuote)
					{
						cell += delim;
					}
					else
					{
						if (!cells.IsEmpty || !cell.IsEmpty)
						{
							cells += cell.ToString();
						}

						lineEnd = true;
						return true;
					}

					break;


				case Quote:
					isInQuote = !isInQuote;
					break;
			}
		}
		else if (isLastBuffer)
		{
			cell += buffer.ToRope();
			cells += cell.ToString();
			bytesConsumed = buffer.End;
			lineEnd = true;
			return true;
		}
		else // no more items in this sequence, wait for more data.
		{
			// Mid cell bail.
			return false;
		}
		
		return true;
	}

	public static SequencePosition ReadCsvLine(in ReadOnlySequence<byte> sequence, bool isCompleted, out Rope<string> cells, out bool lineEnd, out long bytesConsumed)
	{
		var reader = new SequenceReader<byte>(sequence);

		var cell = Rope<byte>.Empty;
		cells = Rope<string>.Empty;
		bool isInQuote = false;
		lineEnd = false;
		bytesConsumed = 0;
		while (!reader.End) // loop until we've read the entire sequence
		{
			if (reader.TryReadToAny(out ReadOnlySpan<byte> itemBytes, Delims, advancePastDelimiter: false)) // we have an item to handle
			{
				bytesConsumed += itemBytes.Length;
				cell = itemBytes.ToArray();
				if (reader.TryRead(out var delim))
				{
					bytesConsumed++;
					switch (delim)
					{
						case Comma:
							if (isInQuote)
							{
								cell += delim;
							}
							else
							{
								cells += cell.ToString();
								cell = Rope<byte>.Empty;
							}

							break;
						case Cr:
						case Lf:
							if (isInQuote)
							{
								cell += delim;
							}
							else
							{
								if (!cells.IsEmpty || !cell.IsEmpty)
								{
									cells += cell.ToString();
								}

								lineEnd = true;
								return reader.Position;
							}

							break;


						case Quote:
							isInQuote = !isInQuote;
							break;
					}
				}
			}
			else if (isCompleted) // read last item which has no final delimiter
			{
				var slice = sequence.Slice(reader.Position);
				bytesConsumed += slice.Length;
				var stringLine = Encoding.UTF8.GetString(slice);
				cells += stringLine;
				lineEnd = true;
				reader.Advance(sequence.Length); // advance reader to the end
			}
			else // no more items in this sequence
			{
				// Mid cell bail.
				break;
			}
		}

		return reader.Position;
	}

	public static IEnumerable<(IReadOnlyList<string> Cells, long StartOffset, long EndOffset)> ReadCsvLines(this TextReader reader)
	{
		long startPosition = 0L;
		int capacity = 10;
		int charsConsumed;
		do 
		{
			var line = reader.ReadCsvLine(capacity, out charsConsumed);
			capacity = line.Count;
			long endPosition = startPosition + charsConsumed;
			if (line.Count > 1 || (line.Count > 0 && line[0].Length != 0))
			{
				yield return (line, startPosition, endPosition);
			}
			
			startPosition = endPosition;
		}
		while (charsConsumed > 0);
	}

	public static List<string> ReadCsvLine(this TextReader reader, int capacity, out int charsConsumed)
	{
		bool isInQuote = false;
		var b = new StringBuilder();
		var list = new List<string>(capacity);
		charsConsumed = 0;
		int x;
		while ((x = reader.Read()) >= 0)
        {
			var c = (char)x;
			charsConsumed++;
			switch (c) 
			{
				case '\r':
				case '\n':
					if (isInQuote)
					{
						b.Append(c);
					}
					else
					{
						while (reader.Peek() == '\n')
						{
							charsConsumed++;
							reader.Read();
						}

						list.Add(b.ToString());
						return list;
					}
					
					break;

				case ',':
					if (isInQuote)
					{
						b.Append(c);
					}
					else
					{
						list.Add(b.ToString());
						b = new StringBuilder();
					}
					
					break;
					
				case '\"':
					isInQuote = !isInQuote;
					break;
					
				default:
					b.Append(c);
					break;
			}
		}

		if (b.Length != 0)
		{
			list.Add(b.ToString());
		}
		
		return list;
	}
}
