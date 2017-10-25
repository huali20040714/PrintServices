using System;
using System.Linq;
using System.Text;
using System.IO;
using Base;
using System.Collections.Specialized;

namespace HTTPServerLib
{
    /// <summary>
    /// HTTP请求定义
    /// </summary>
    public class HttpRequest : BaseHeader
    {
        /// <summary>
        /// HTTP请求方式
        /// </summary>
        public Method Method { get; private set; }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public NameValueCollection Params { get; private set; } = new NameValueCollection();

        const int MAX_SIZE = 1024 * 1024 * 2;

        /// <summary>
        /// 定义缓冲区
        /// </summary>
        private byte[] bytes = new byte[MAX_SIZE];

        /// <summary>
        /// 客户端请求报文
        /// </summary>
        private string content = "";

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="handler">客户端Socket</param>
        public HttpRequest(Stream handler)
        {
            int length = 0;
            do
            {
                //缓存客户端请求报文
                length = handler.Read(bytes, 0, MAX_SIZE);
                content += Encoding.UTF8.GetString(bytes, 0, length);
            } while (length > MAX_SIZE);

            if (string.IsNullOrEmpty(content)) return;

            //按行分割请求报文
            string[] lines = content.Split('\n');

            //获取请求方法
            var firstLine = lines[0].Split(' ');

            if (firstLine.Length > 0)
            {
                if (firstLine[0] == Method.GET.ToString())
                    this.Method = Method.GET;
                else if (firstLine[0] == Method.POST.ToString())
                    this.Method = Method.POST;
                else
                    this.Method = 0;
            }

            if (firstLine.Length > 1)
            {
                this.URL = Uri.UnescapeDataString(firstLine[1]);
            }

            //获取请求参数
            if (this.Method == Method.GET && this.URL.Contains('?'))
            {
                this.Params = GetRequestParams(URL.Split('?')[1]);
            }
            else if (this.Method == Method.POST)
            {
                this.Params = GetRequestParams(lines[lines.Length - 1]);
            }
        }

        /// <summary>
        /// 构建请求头部
        /// </summary>
        /// <returns></returns>
        public string BuildHeader()
        {
            return this.content;
        }
    }
}
