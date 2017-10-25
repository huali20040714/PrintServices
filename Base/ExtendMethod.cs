using System.Collections.Generic;

namespace Base
{
    /// <summary>
    /// 扩展方法类
    /// </summary>
    public static class ExtendMethod
    {
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key)
        {
            if (dic != null && dic.ContainsKey(key))
                return dic[key];
            else
                return default(TValue);
        }
    }
}
