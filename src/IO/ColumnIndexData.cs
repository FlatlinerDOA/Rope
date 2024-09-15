namespace Rope.IO;
using Rope;

public record class ColumnIndexData(string Name, Rope<RowRange> Ranges);
