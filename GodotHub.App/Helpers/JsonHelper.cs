using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GodotHub.App.Helpers;

public class JsonIncludeConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var instance = Activator.CreateInstance<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return instance;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;
            
            var propertyName = reader.GetString();

            reader.Read();

            if (propertyName == null) 
                continue;
                
            var property = typeToConvert.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var field = typeToConvert.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null && property.GetCustomAttribute<JsonIncludeAttribute>() != null)
            {
                var propertyValue = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                property.SetValue(instance, propertyValue);
            }
            else if (field != null && field.GetCustomAttribute<JsonIncludeAttribute>() != null)
            {
                var fieldValue = JsonSerializer.Deserialize(ref reader, field.FieldType, options);
                field.SetValue(instance, fieldValue);
            }
            else
                reader.Skip();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<JsonIncludeAttribute>() != null);

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(value);
            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }

        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<JsonIncludeAttribute>() != null);

        foreach (var field in fields)
        {
            var fieldValue = field.GetValue(value);
            writer.WritePropertyName(field.Name);
            JsonSerializer.Serialize(writer, fieldValue, field.FieldType, options);
        }

        writer.WriteEndObject();
    }
}