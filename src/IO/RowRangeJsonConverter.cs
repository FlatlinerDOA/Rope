namespace Rope.IO;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class RowRangeJsonConverter : JsonConverter<RowRange>
{
	public int BloomFilterSize  { get; init; }
	public int HashIterations { get; init; }
    public SupportedOperationFlags SupportedOperations { get; init; }

    public override RowRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		long startBytePosition = 0;
		long endBytePosition = 0;
		int startRowIndex = 0;
		int endRowIndex = 0;
		BloomFilter? filter = null;
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject && filter != null)
			{
				return new RowRange()
				{

					StartBytePosition  = startBytePosition,
					EndBytePosition = endBytePosition,
					StartRowIndex = startRowIndex,
					EndRowIndex = endRowIndex,
					Filter = filter
				 };
			}

			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string? propertyName = reader.GetString();
				reader.Read();
				switch (propertyName)
				{
					case "s":
						startBytePosition = reader.GetInt64();
						break;
					case "e":
						endBytePosition = reader.GetInt64();
						break;
					case "sr":
						startRowIndex = reader.GetInt32();
						break;
					case "er":
						endRowIndex = reader.GetInt32();
						break;
					case "f":
                        filter = new BloomFilter(this.BloomFilterSize, this.HashIterations, this.SupportedOperations, reader.GetString()!);
						break;
                    default:
                        break;
                }
            }
		}
		
		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, RowRange value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("s", value.StartBytePosition);
		writer.WriteNumber("e", value.EndBytePosition);
		writer.WriteNumber("sr", value.StartRowIndex);
		writer.WriteNumber("er", value.EndRowIndex);
		writer.WritePropertyName("f");
		JsonSerializer.Serialize(writer, value.Filter.SerializedBits, options);
		writer.WriteEndObject();
	}
}
