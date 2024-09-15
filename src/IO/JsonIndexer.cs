
namespace Rope.IO;

// internal class StreamPipeReaderOptions
// {
//     private int bufferSize;

//     public StreamPipeReaderOptions(int bufferSize)
//     {
//         this.bufferSize = bufferSize;
//     }
// }

public class JsonIndexer : Indexer
{
	public override string FileExtension => ".json";

    public override ValueTask<FileIndex> IndexFile(string filePath, DateTimeOffset lastWriteTimeUtc, TextReader reader, CancellationToken cancellation)
    {
        throw new NotImplementedException();
    }

    //	private async Task<FileIndex> IndexJsonLines(string filePath)
    //	{
    //		var pipeReader = PipeReader.Create(Stream, new StreamPipeReaderOptions(bufferSize: 32));
    //		while (true)
    //		{
    //			var result = await pipeReader.ReadAsync(); // read from the pipe
    //
    //			var buffer = result.Buffer;
    //
    //			var position = ReadItems(buffer, result.IsCompleted); // read complete items from the current buffer
    //
    //			if (result.IsCompleted)
    //				break; // exit if we've read everything from the pipe
    //
    //			pipeReader.AdvanceTo(position, buffer.End); //advance our position in the pipe
    //		}
    //
    //		pipeReader.Complete(); // mark the PipeReader as complete
    //	}
}
