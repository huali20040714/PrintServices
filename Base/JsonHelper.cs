using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Base
{
    public class JsonHelper
    {
        private static readonly Type TypeOfInt = typeof(int);
        private static readonly Type TypeOfLong = typeof(long);
        private static readonly Type TypeOfString = typeof(string);
        private static readonly Type TypeOfFloat = typeof(float);
        private static readonly Type TypeOfDouble = typeof(double);
        private static readonly Type TypeOfDecimal = typeof(decimal);

        private static bool IsSystemDataType(Type typeOfT)
        {
            if (typeOfT == TypeOfInt ||
                typeOfT == TypeOfLong ||
                typeOfT == TypeOfFloat ||
                typeOfT == TypeOfDouble ||
                typeOfT == TypeOfString ||
                typeOfT == TypeOfDecimal)
                return true;
            return false;
        }

        /// <summary>
        /// 将指定的对象序列化成 JSON 数据。
        /// </summary>
        /// <param name="obj">要序列化的对象。</param>
        /// <returns></returns>
        public static string ToJson(object obj)
        {
            if (null == obj)
                return null;

            if (IsSystemDataType(obj.GetType()))
                obj.ToString();

            IsoDateTimeConverter datetimeConverter = new IsoDateTimeConverter();
            datetimeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();

            _jsonSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            _jsonSettings.NullValueHandling = NullValueHandling.Ignore;
            _jsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            _jsonSettings.Converters.Add(datetimeConverter);

            return JsonConvert.SerializeObject(obj, Formatting.None, _jsonSettings);
        }
        
        /// <summary>
        /// 将指定的 JSON 数据反序列化成指定对象。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="json">JSON 数据。</param>
        /// <returns></returns>
        public static T FromJson<T>(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return default(T);

                IsoDateTimeConverter datetimeConverter = new IsoDateTimeConverter();
                datetimeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
                JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();

                _jsonSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                _jsonSettings.NullValueHandling = NullValueHandling.Ignore;
                _jsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                _jsonSettings.Converters.Add(datetimeConverter);
                return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
