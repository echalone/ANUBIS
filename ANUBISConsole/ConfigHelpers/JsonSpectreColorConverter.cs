using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANUBISConsole.ConfigHelpers
{
#pragma warning disable CS8625, CS8600, CS8603
    public class JsonSpectreColorConverter : JsonConverter<Color>
    {
        public JsonSpectreColorConverter() { }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Color).IsAssignableFrom(typeToConvert);
        }

        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }
            else
            {
                if (Style.TryParse(value, out var style))
                {
                    return style?.Foreground ?? default;
                }
                else
                {
                    return default;
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            var strValue = value.ToString();

            writer.WriteStringValue(strValue);
        }
    }
#pragma warning restore CS8625, CS8600, CS8603
}
