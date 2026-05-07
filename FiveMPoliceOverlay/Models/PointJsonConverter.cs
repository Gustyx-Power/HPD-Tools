using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace FiveMPoliceOverlay.Models
{
    /// <summary>
    /// JSON converter for System.Windows.Point to serialize as {x, y} object
    /// </summary>
    public class PointJsonConverter : JsonConverter<Point>
    {
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            double x = 0;
            double y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Point(x, y);
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = reader.GetDouble();
                            break;
                        case "y":
                            y = reader.GetDouble();
                            break;
                    }
                }
            }

            throw new JsonException("Expected EndObject token");
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }
    }
}
