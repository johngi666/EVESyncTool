using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EVESyncTool.Core.Utils
{
    /// <summary>
    /// JSON 操作辅助类
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions CompactOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        #region 序列化

        /// <summary>
        /// 将对象序列化为 JSON 字符串（格式化）
        /// </summary>
        public static string Serialize<T>(T obj, bool pretty = true)
        {
            if (obj == null)
                return "null";

            var options = pretty ? DefaultOptions : CompactOptions;
            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// 将对象序列化为 JSON 并写入文件
        /// </summary>
        public static void SerializeToFile<T>(T obj, string filePath, bool pretty = true)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            FileHelper.EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            string json = Serialize(obj, pretty);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 异步将对象序列化为 JSON 并写入文件
        /// </summary>
        public static async Task SerializeToFileAsync<T>(T obj, string filePath, bool pretty = true)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            FileHelper.EnsureDirectoryExists(Path.GetDirectoryName(filePath));
            string json = Serialize(obj, pretty);
            await FileHelper.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }

        #endregion

        #region 反序列化

        /// <summary>
        /// 从 JSON 字符串反序列化为对象
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }

        /// <summary>
        /// 从文件读取 JSON 并反序列化为对象
        /// </summary>
        public static T DeserializeFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Deserialize<T>(json);
        }

        /// <summary>
        /// 异步从文件读取 JSON 并反序列化为对象
        /// </summary>
        public static async Task<T> DeserializeFromFileAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            string json = await FileHelper.ReadAllTextAsync(filePath, Encoding.UTF8);
            return Deserialize<T>(json);
        }

        /// <summary>
        /// 安全反序列化（失败时返回默认值）
        /// </summary>
        public static T DeserializeSafe<T>(string json, T defaultValue = default)
        {
            try
            {
                return Deserialize<T>(json);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 从文件安全反序列化（失败时返回默认值）
        /// </summary>
        public static T DeserializeFromFileSafe<T>(string filePath, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(filePath))
                    return defaultValue;
                return DeserializeFromFile<T>(filePath);
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region 路径操作

        /// <summary>
        /// 按路径获取 JSON 值
        /// </summary>
        public static JsonElement GetValueByPath(JsonElement root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            string[] segments = path.Split('.');
            JsonElement current = root;

            foreach (string segment in segments)
            {
                try
                {
                    if (current.ValueKind == JsonValueKind.Object)
                    {
                        if (current.TryGetProperty(segment, out JsonElement next))
                        {
                            current = next;
                            continue;
                        }

                        if (segment.StartsWith("bytes:"))
                        {
                            string key = segment.Substring(6);
                            if (current.TryGetProperty(key, out JsonElement next2))
                            {
                                current = next2;
                                continue;
                            }
                        }

                        if (!segment.StartsWith("bytes:"))
                        {
                            string prefixedKey = $"bytes:{segment}";
                            if (current.TryGetProperty(prefixedKey, out JsonElement next3))
                            {
                                current = next3;
                                continue;
                            }
                        }

                        return default;
                    }

                    if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out int index))
                    {
                        if (index >= 0 && index < current.GetArrayLength())
                        {
                            current = current[index];
                            continue;
                        }
                    }

                    return default;
                }
                catch
                {
                    return default;
                }
            }

            return current;
        }

        /// <summary>
        /// 尝试按路径获取 JSON 值
        /// </summary>
        public static bool TryGetValueByPath(JsonElement root, string path, out JsonElement value)
        {
            value = GetValueByPath(root, path);
            return value.ValueKind != JsonValueKind.Undefined;
        }

        /// <summary>
        /// 按路径获取 JSON 字符串值
        /// </summary>
        public static string GetStringByPath(JsonElement root, string path, string defaultValue = "")
        {
            JsonElement element = GetValueByPath(root, path);
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? defaultValue;
            return defaultValue;
        }

        /// <summary>
        /// 按路径获取 JSON 整数值
        /// </summary>
        public static int GetIntByPath(JsonElement root, string path, int defaultValue = 0)
        {
            JsonElement element = GetValueByPath(root, path);
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value))
                return value;
            return defaultValue;
        }

        /// <summary>
        /// 按路径获取 JSON 布尔值
        /// </summary>
        public static bool GetBoolByPath(JsonElement root, string path, bool defaultValue = false)
        {
            JsonElement element = GetValueByPath(root, path);
            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                return element.GetBoolean();
            return defaultValue;
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证 JSON 字符串是否有效
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证 JSON 字符串是否有效（返回错误信息）
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return (false, "JSON 内容为空");

            try
            {
                JsonDocument.Parse(json);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// 验证 JSON 文件是否有效
        /// </summary>
        public static bool IsValidJsonFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                return IsValidJson(json);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 合并

        /// <summary>
        /// 合并两个 JSON 对象（深度合并）
        /// </summary>
        public static string MergeJson(string baseJson, string overrideJson)
        {
            if (string.IsNullOrEmpty(baseJson))
                return overrideJson;
            if (string.IsNullOrEmpty(overrideJson))
                return baseJson;

            using var baseDoc = JsonDocument.Parse(baseJson);
            using var overrideDoc = JsonDocument.Parse(overrideJson);

            var merged = MergeElements(baseDoc.RootElement, overrideDoc.RootElement);
            return JsonSerializer.Serialize(merged, DefaultOptions);
        }

        private static object MergeElements(JsonElement baseElement, JsonElement overrideElement)
        {
            if (overrideElement.ValueKind == JsonValueKind.Undefined)
                return ConvertElement(baseElement);

            if (baseElement.ValueKind == JsonValueKind.Undefined)
                return ConvertElement(overrideElement);

            if (baseElement.ValueKind != JsonValueKind.Object || overrideElement.ValueKind != JsonValueKind.Object)
                return ConvertElement(overrideElement);

            var result = new Dictionary<string, object>();

            foreach (var prop in baseElement.EnumerateObject())
            {
                result[prop.Name] = ConvertElement(prop.Value);
            }

            foreach (var prop in overrideElement.EnumerateObject())
            {
                if (result.ContainsKey(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object &&
                    result[prop.Name] is Dictionary<string, object> dict)
                {
                    var overrideObj = ConvertElement(prop.Value) as Dictionary<string, object>;
                    if (overrideObj != null)
                    {
                        foreach (var kvp in overrideObj)
                        {
                            dict[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else
                {
                    result[prop.Name] = ConvertElement(prop.Value);
                }
            }

            return result;
        }

        private static object ConvertElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertElement(prop.Value);
                    }
                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertElement(item));
                    }
                    return list;

                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long longVal))
                        return longVal;
                    if (element.TryGetDouble(out double doubleVal))
                        return doubleVal;
                    return element.GetRawText();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return element.GetRawText();
            }
        }

        #endregion

        #region 格式化

        /// <summary>
        /// 格式化 JSON 字符串
        /// </summary>
        public static string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, DefaultOptions);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// 压缩 JSON 字符串（移除空白）
        /// </summary>
        public static string MinifyJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, CompactOptions);
            }
            catch
            {
                return json;
            }
        }

        #endregion
    }
}