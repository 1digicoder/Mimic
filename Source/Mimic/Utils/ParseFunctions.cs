using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mimic.Utils
{
    /// <summary>
    /// Provides the set of functions that are used in the parsing pipeline. 
    /// The parsing pipeline is defined in ServiceFunctions.cs and is used to 
    /// parse the body of add service requests.
    /// </summary>
    public static class ParseFunctions
    {

        /// <summary>
        /// Reads the next line context's StreamReader, and stores it on the 
        /// context's Input property before calling the next function in the 
        /// pipeline.
        /// </summary>
        public static Task ReadLine(ParseContext context, Func<Task> next)
        {
            RequiresArgument.NotNull(context, "context");
            RequiresArgument.NotNull(next, "next");

            context.Input = context.Reader.ReadLine().Trim();
            return next();
        }

        /// <summary>
        /// Stops the current pass through the pipeline when the context's 
        /// Input property is NULL, white space, or a comment, e.g. "# 
        /// Comments begin with a hash mark".
        /// </summary>
        public static Task IgnoreCommentsAndBlankLines(ParseContext context, Func<Task> next)
        {
            RequiresArgument.NotNull(context, "context");
            RequiresArgument.NotNull(next, "next");

            if (string.IsNullOrWhiteSpace(context.Input) || context.Input.StartsWith("#"))
            {
                return Task.CompletedTask;
            }
            else
            {
                return next();
            }
        }

        /// <summary>
        /// Attempts to parse a name-value pair from the context's Input 
        /// property. This is the last function in the pipeline, so it never 
        /// invokes the specified next function.
        /// </summary>
        public static Task ParseSetting(ParseContext context, Func<Task> next)
        {
            const int SettingName = 0;
            const int SettingValue = 1;
            const int MaxSubstrings = 2;

            RequiresArgument.NotNull(context, "context");
            RequiresArgument.NotNull(next, "next");

            var arr = context
                .Input
                // We only want to split on the first colon, so the maximum 
                //number of resulting substrings is limited to two.
                .Split(new[] { ':' }, MaxSubstrings)
                .Select(x => x.Trim())
                .ToArray();

            RequiresArgument.LengthEquals(arr, MaxSubstrings, "Settings must have a name and value. For example: MyFavoriteColor: Red");
            RequiresArgument.NotNullOrWhiteSpace(arr[SettingName], "Setting name");
            RequiresArgument.NotNullOrWhiteSpace(arr[SettingValue], "Setting value");

            // Using reflection to find parsed setting on the context's state 
            // and set its value.
            var field = context.State.GetType().GetRuntimeField(arr[SettingName]);
            RequiresSettingExists(field, arr[SettingName]);
            field.SetValue(context.State, arr[SettingValue]);

            return Task.CompletedTask;
        }

        /// <summary>
        /// When the context's Input is "# Body", reads the rest of the text 
        /// from the context's StreamReader and interpret's it as the desired 
        /// response body for the virtual service being defined.
        /// </summary>
        public static Task ParseBody(ParseContext context, Func<Task> next)
        {
            RequiresArgument.NotNull(context, "context");
            RequiresArgument.NotNull(next, "next");

            if (context.Input == ("# Body"))
            {
                var field = context.State.GetType().GetRuntimeField("Body");
                field.SetValue(context.State, context.Reader.ReadToEnd());
                return Task.CompletedTask;
            }
            else
            {
                return next();
            }
        }
		public static HeaderDictionary ToHeaderDictionary(string json)
        {
			var obj = JsonConvert.DeserializeObject<HeaderDictionary>(json, new HeaderJsonConverter());

			return obj;
        }

        public static string ToJsonString(HeaderDictionary headers)
        {
			var json = JsonConvert.SerializeObject(headers, new HeaderJsonConverter());

			return json;
        }

        private static void RequiresSettingExists(FieldInfo field, string settingName)
        {
            if (field == null)
            {
                throw new ArgumentException($"Unknown setting: '{settingName}'. Please check your spelling, and be aware that setting names are case sensitive.");
            }
        }
    }

	public class HeaderJsonConverter : JsonConverter<HeaderDictionary>
	{
		public override void WriteJson(JsonWriter writer, HeaderDictionary value, JsonSerializer serializer)
		{


			writer.WriteStartArray();
			foreach(var item in value)
			{
                
				writer.WriteStartObject();
				writer.WritePropertyName(item.Key);
				writer.WriteStartArray();
				foreach (var subItem in item.Value)
				{
					writer.WriteValue(subItem);
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}

		public override HeaderDictionary ReadJson(JsonReader reader, Type objectType, HeaderDictionary existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var r = new HeaderDictionary();

            if (reader.TokenType == JsonToken.StartArray)
			{
				reader.Read();
				while (reader.TokenType != JsonToken.EndArray)
				{
                    if (reader.TokenType != JsonToken.StartObject)
                    {
                        throw new JsonSerializationException("Unexpected Token");
                    }
					r.Add(ExtractKvp(reader));                   
				}
			}

			return r;
		}

		private static KeyValuePair<string, StringValues> ExtractKvp(JsonReader reader)
		{
			string key;
			ICollection<string> values = new List<string>();

			reader.Read();
			if (reader.TokenType != JsonToken.PropertyName)
			{
				throw new JsonSerializationException("Unexpected token.");
			}
			key = (string)reader.Value;
			reader.Read();
			if (reader.TokenType != JsonToken.StartArray)
			{
				throw new JsonSerializationException("Unexpected token.");
			}
			reader.Read();
			while (reader.TokenType != JsonToken.EndArray)
			{
				if (reader.TokenType != JsonToken.String)
				{
					throw new JsonSerializationException("Unexpected token.");
				}
				values.Add((string)reader.Value);
				reader.Read();
			}
			reader.Read();
			if (reader.TokenType != JsonToken.EndObject)
            {
                throw new JsonSerializationException("Unexpected Token.");
			}
			reader.Read();
			return new KeyValuePair<string, StringValues>(key, values.ToArray());
		}
	}
}
