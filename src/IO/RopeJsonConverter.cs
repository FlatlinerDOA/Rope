namespace Rope.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rope;

public sealed class RopeJsonConverter<T> : JsonConverter<Rope<T>> where T : IEquatable<T>
{
	public override Rope<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var rowRange = Rope<T>.Empty;
		var elementConverter = options.GetConverter(typeof(T)) as JsonConverter<T>;
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
			{
				return rowRange;
			}

			rowRange += elementConverter.Read(ref reader, typeof(T), options);			
		}

		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, Rope<T> value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}
