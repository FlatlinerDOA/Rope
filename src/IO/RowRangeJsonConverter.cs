namespace Rope.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class RowRangeJsonConverter : JsonConverter<RowRange>
{
	public int BloomFilterSize  { get; init; }
	public int HashFunctions { get; init; }
	
	public override RowRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var rowRange = new RowRange();
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return rowRange;
			}

			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string propertyName = reader.GetString();
				reader.Read();
				switch (propertyName)
				{
					case "s":
						rowRange.StartBytePosition = reader.GetInt64();
						break;
					case "e":
						rowRange.EndBytePosition = reader.GetInt64();
						break;
					case "sr":
						rowRange.StartRowIndex = reader.GetInt32();
						break;
					case "er":
						rowRange.EndRowIndex = reader.GetInt32();
						break;
					case "f":
						rowRange.Filter = new BloomFilter(this.BloomFilterSize, this.HashFunctions, reader.GetString(), SupportedOperationFlags.StartsWith);
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
