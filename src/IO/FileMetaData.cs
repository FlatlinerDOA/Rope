namespace Rope.IO;
using Rope;

public record class FileMetaData(string Path, DateTimeOffset LastModifiedUtc, Rope<string> Headers);
