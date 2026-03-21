using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using xinglin.Models.CoreEntities;

namespace xinglin.Services.Data
{
    /// <summary>
    /// 支持 ControlElement / TableElement 多态反序列化的 JsonConverter。
    /// 当 JSON 中 Type 字段为 "Table" 或 "6" 时，反序列化为 TableElement；否则为 ControlElement。
    /// </summary>
    public class ControlElementConverter : JsonConverter<ControlElement>
    {
        public override ControlElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            bool isTable = false;
            if (root.TryGetProperty("Type", out var typeProp))
            {
                if (typeProp.ValueKind == JsonValueKind.String)
                {
                    var typeStr = typeProp.GetString();
                    isTable = string.Equals(typeStr, "Table", StringComparison.OrdinalIgnoreCase)
                           || typeStr == "6";
                }
                else if (typeProp.ValueKind == JsonValueKind.Number)
                {
                    isTable = typeProp.GetInt32() == (int)ControlType.Table;
                }
            }

            // 创建不含本 converter 的选项，避免无限递归
            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Remove(this);

            var rawText = root.GetRawText();
            if (isTable)
                return JsonSerializer.Deserialize<TableElement>(rawText, innerOptions)!;

            return JsonSerializer.Deserialize<ControlElement>(rawText, innerOptions)!;
        }

        public override void Write(Utf8JsonWriter writer, ControlElement value, JsonSerializerOptions options)
        {
            // 创建不含本 converter 的选项，避免无限递归
            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Remove(this);

            if (value is TableElement tableElement)
                JsonSerializer.Serialize(writer, tableElement, innerOptions);
            else
                JsonSerializer.Serialize(writer, value, innerOptions);
        }
    }
}
