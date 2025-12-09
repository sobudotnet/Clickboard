using System.Drawing;
using Newtonsoft.Json;

public class Theme
{
    public string Name { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color BackgroundColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color ButtonColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color ButtonTextColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color InputFieldColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color InputFieldTextColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color HeaderBarColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color HeaderBarTextColor { get; set; }
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color TitleBarColor { get; set; } 
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color BorderColor { get; set; } = Color.Black;
    public int BorderThickness { get; set; } = 2;
}

public class ColorJsonConverter : JsonConverter {
    public override bool CanConvert(System.Type objectType) => objectType == typeof(Color);
    public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
    {
        var value = reader.Value as string;
        if (string.IsNullOrWhiteSpace(value))
            return Color.Empty;
        return ColorTranslator.FromHtml(value);
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var color = (Color)value;
        string html = ColorTranslator.ToHtml(color);
        writer.WriteValue(html);
    }
}
