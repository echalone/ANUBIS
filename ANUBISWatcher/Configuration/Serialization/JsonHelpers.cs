using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANUBISWatcher.Configuration.Serialization
{
    #region Enum serialization

    public class JsonEnumNullValueAttribute : JsonAttribute
    {
    }

    public class JsonEnumErrorValueAttribute : JsonAttribute
    {
    }

    public class JsonEnumNameAttribute : JsonAttribute
    {
        public List<string> Names { get; init; }

        public bool IgnoreCase { get; init; }

        public string? WriteName { get { return Names?.FirstOrDefault(); } }

        public JsonEnumNameAttribute(params string[] names)
            : this(false, names)
        {
        }

        public JsonEnumNameAttribute(bool ignoreCase, params string[] names)
        {
            IgnoreCase = ignoreCase;
            Names = new List<string>(names ?? []);
        }

        public bool InNames(string value)
        {
            return Names.Any(itm => string.Equals(itm, value,
                                                    IgnoreCase ?
                                                        StringComparison.InvariantCultureIgnoreCase :
                                                        StringComparison.InvariantCulture));
        }
    }

#pragma warning disable CS8625, CS8600, CS8603
    public class JsonEnumConverter<T> : JsonConverter<T>
    {
        private readonly JsonConverter<T>? _converter;
        private readonly Type _underlyingType;

        public JsonEnumConverter() : this(null) { }

        public JsonEnumConverter(JsonSerializerOptions options)
        {
            // for performance, use the existing converter if available
            if (options != null)
                _converter = (JsonConverter<T>)options.GetConverter(typeof(T));

            // cache the underlying type
            _underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_converter != null)
            {
                return _converter.Read(ref reader, _underlyingType, options) ?? default;
            }

            string? value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                var nullName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNullValueAttribute>(_underlyingType).FirstOrDefault().Name;

                if (!string.IsNullOrWhiteSpace(nullName))
                    return Enum.TryParse(_underlyingType, nullName, out var parsedResult) ? (T)parsedResult : default;
                else
                    return default;
            }

            var enumName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                            .FirstOrDefault(item => item.Attribute.InNames(value)).Name;


            // for performance, parse with ignoreCase:false first.
            if ((enumName == null || !Enum.TryParse(_underlyingType, enumName, ignoreCase: false, out object result))
                    && !Enum.TryParse(_underlyingType, value, ignoreCase: false, out result)
                    && !Enum.TryParse(_underlyingType, value, ignoreCase: true, out result))
            {
                var errorName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumErrorValueAttribute>(_underlyingType).FirstOrDefault().Name;

                if (!string.IsNullOrWhiteSpace(errorName))
                {
                    using (SharedData.ConfigLogging?.BeginScope("ConfigFileManager"))
                    {
                        SharedData.ConfigLogging?.LogError(@"Configuration file error: Could not parse enum value ""{value}"" of type {type}, will use default error value ""{errorvalue}""", value, _underlyingType, errorName);
                        return Enum.TryParse(_underlyingType, errorName, out var parsedResult) ? (T)parsedResult : default;
                    }
                }
                else
                {
                    throw new JsonException($"Unable to convert \"{value}\" to enum \"{_underlyingType}\".");
                }
            }

            return (T)result;
        }

        public override void Write(Utf8JsonWriter writer,
            T value, JsonSerializerOptions options)
        {
            var strValue = value?.ToString();

            if (!string.IsNullOrWhiteSpace(strValue))
            {
                var enumValue = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                        .FirstOrDefault(item => item.Name == strValue).Attribute?.WriteName;

                if (enumValue != null)
                    strValue = enumValue;
            }

            writer.WriteStringValue(strValue);
        }

#pragma warning disable CS0693, CS8619
        private static (string Name, T Attribute)[] GetMemberAttributePairs<T>(Type type) where T : JsonAttribute
        {
            return type.GetMembers()
                    .Select(member => (member.Name, Attribute: (T)member.GetCustomAttributes(typeof(T), false).FirstOrDefault()))
                    .Where(p => p.Attribute != null).ToArray();
        }
#pragma warning restore CS0693, CS8619
    }
#pragma warning restore CS8625, CS8600, CS8603

#pragma warning disable CS8625, CS8600, CS8603

    public class JsonEnumArrayConverter<T, TEnm> : JsonConverter<T> where T : IEnumerable<TEnm>
    {
        private readonly JsonConverter<T>? _converter;
        private readonly Type _underlyingType;

        public JsonEnumArrayConverter() : this(null) { }

        public JsonEnumArrayConverter(JsonSerializerOptions options)
        {
            Type tpArray = typeof(T).GetElementType() ?? typeof(T);

            // for performance, use the existing converter if available
            if (options != null)
                _converter = (JsonConverter<T>)options.GetConverter(tpArray);

            // cache the underlying type
            _underlyingType = Nullable.GetUnderlyingType(tpArray) ?? tpArray;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<TEnm> lstValues = [];

            if (_converter != null)
            {
                return _converter.Read(ref reader, _underlyingType, options) ?? default;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
            }

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                string? value = reader.GetString();

                object result = Activator.CreateInstance(_underlyingType);

                if (string.IsNullOrWhiteSpace(value))
                {
                    var nullName = JsonEnumArrayConverter<T, TEnm>.GetMemberAttributePairs<JsonEnumNullValueAttribute>(_underlyingType).FirstOrDefault().Name;

                    if (!string.IsNullOrWhiteSpace(nullName))
                        result = Enum.TryParse(_underlyingType, nullName, out var parsedResult) ? parsedResult : Activator.CreateInstance(_underlyingType);
                    else
                        result = Activator.CreateInstance(_underlyingType);
                }
                else
                {
                    var enumName = JsonEnumArrayConverter<T, TEnm>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                                    .FirstOrDefault(item => item.Attribute.InNames(value)).Name;

                    // for performance, parse with ignoreCase:false first.
                    if ((enumName == null || !Enum.TryParse(_underlyingType, enumName, ignoreCase: false, out result))
                            && !Enum.TryParse(_underlyingType, value, ignoreCase: false, out result)
                            && !Enum.TryParse(_underlyingType, value, ignoreCase: true, out result))
                    {

                        var errorName = JsonEnumArrayConverter<T, TEnm>.GetMemberAttributePairs<JsonEnumErrorValueAttribute>(_underlyingType).FirstOrDefault().Name;

                        if (!string.IsNullOrWhiteSpace(errorName))
                        {
                            using (SharedData.ConfigLogging?.BeginScope("ConfigFileManager"))
                            {
                                SharedData.ConfigLogging?.LogError(@"Configuration file error: Could not parse enum value ""{value}"" of type {type}, will use default error value ""{errorvalue}""", value, _underlyingType, errorName);
                                result = Enum.TryParse(_underlyingType, errorName, out var parsedResult) ? parsedResult : Activator.CreateInstance(_underlyingType);
                            }
                        }
                        else
                            throw new JsonException($"Unable to convert \"{value}\" to enum \"{_underlyingType}\".");
                    }
                }

                if (result != null)
                    lstValues.Add((TEnm)result);

                reader.Read();
            }

            return (T)(IEnumerable)lstValues.ToArray();
        }

        public override void Write(Utf8JsonWriter writer,
            T value, JsonSerializerOptions options)
        {
            //string strValue = null;
            List<string> lstValues = [];

            if (value?.GetType().IsArray ?? false)
            {
                //strValue = "[";
                //string strSeparator = "";
                IEnumerator enm = value.GetEnumerator();



                while (enm.MoveNext())
                {
                    object val = enm.Current;
                    string strEnumVal = val.ToString();
                    if (!string.IsNullOrWhiteSpace(strEnumVal))
                    {
                        var enumValue = JsonEnumArrayConverter<T, TEnm>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                                .FirstOrDefault(item => item.Name == strEnumVal).Attribute?.WriteName;

                        if (enumValue != null)
                            strEnumVal = enumValue;

                        lstValues.Add(strEnumVal);
                        //strValue += strSeparator + strEnumVal.ToString();
                        //strSeparator = ", ";
                    }
                    else
                    {
                        var nullName = JsonEnumArrayConverter<T, TEnm>.GetMemberAttributePairs<JsonEnumNullValueAttribute>(_underlyingType).FirstOrDefault().Name;

                        if (nullName != null)
                        {
                            lstValues.Add(nullName);
                            //strValue += strSeparator + nullName.ToString();
                            //strSeparator = ", ";
                        }
                    }
                }

                //strValue += "]";
            }

            writer.WriteStartArray();
            foreach (string strValue in lstValues)
            {
                writer.WriteStringValue(strValue);
            }
            writer.WriteEndArray();

            //writer.WriteStringValue(strValue ?? "[]");
        }

#pragma warning disable CS0693, CS8619
        private static (string Name, T Attribute)[] GetMemberAttributePairs<T>(Type type) where T : JsonAttribute
        {
            return type.GetMembers()
                    .Select(member => (member.Name, Attribute: (T)member.GetCustomAttributes(typeof(T), false).FirstOrDefault()))
                    .Where(p => p.Attribute != null).ToArray();
        }
#pragma warning restore CS0693, CS8619
    }
#pragma warning restore CS8625, CS8600, CS8603

    #endregion
}