using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Base;

namespace HTTPServerLib
{
    public class HttpServer
    {
        // 所有控制器的类型集合
        private static IList<Type> alloctionControllerTypes = new List<Type>();

        #region 公共属性

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIP { get; private set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; private set; }

        /// <summary>
        /// 是否运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 服务端Socet
        /// </summary>
        private TcpListener serverListener;

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public HttpServer(string ipAddress, int port)
        {
            this.ServerIP = ipAddress;
            this.ServerPort = port;

            //获取当前已加载的所有程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                // 遍历所有的类型
                foreach (Type type in types)
                {
                    // 如果当前类型满足条件
                    if (type.IsClass && !type.IsAbstract && type.IsPublic && typeof(Controller).IsAssignableFrom(type))
                    {
                        // 将所有Controller加入集合
                        alloctionControllerTypes.Add(type);
                    }
                }
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 开启服务器
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            this.IsRunning = true;

            Task.Run(() =>
            {
                try
                {
                    //创建服务端Socket
                    this.serverListener = new TcpListener(IPAddress.Parse(ServerIP), ServerPort);
                    this.serverListener.Start();
                    this.Log($"Sever is running at http://{ServerIP}:{ServerPort}");

                    while (IsRunning)
                    {
                        TcpClient client = serverListener.AcceptTcpClient();

                        Thread requestThread = new Thread(() =>
                        {
                            try
                            {
                                ProcessRequest(client);
                            }
                            catch (Exception ex)
                            {
                                Log(ex.Message);
                            }
                            finally
                            {
                                Thread.Sleep(200);
                                client.Close();
                            }
                        });
                        requestThread.Start();

                    }
                }
                catch (Exception e)
                {
                    Log(e.Message);
                }
            });
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            try
            {
                if (!this.IsRunning) return;
                this.IsRunning = false;
                this.serverListener?.Stop();
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 处理客户端请求
        /// </summary>
        /// <param name="handler">客户端Socket</param>
        private void ProcessRequest(TcpClient handler)
        {
            //处理请求
            Stream clientStream = handler.GetStream();
            if (clientStream == null) return;

            //构造HTTP请求
            HttpRequest request = new HttpRequest(clientStream);
            request.Logger = this.Logger;

            //构造HTTP响应
            HttpResponse response = new HttpResponse(clientStream);
            response.Logger = this.Logger;

            //处理请求(映射)
            var executePaths = request.URL.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (executePaths.Length >= 2 && !string.IsNullOrEmpty(executePaths[0]) && !string.IsNullOrEmpty(executePaths[1]))
            {
                var controllerName = executePaths[0];
                var actionName = executePaths[1];
                foreach (var type in alloctionControllerTypes)
                {
                    var action = type.GetMethods().FirstOrDefault(m =>
                    {
                        return m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase) && m.IsPublic && typeof(Result).IsAssignableFrom(m.ReturnType);
                    });
                    if (action != null && type.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase))
                    {
                        var controller = Activator.CreateInstance(type) as Controller;
                        controller.Logger = this.Logger;
                        controller.Request = request;
                        controller.Response = response;
                        var result = action.Invoke(controller, null) as Result;
                        if (result != null)
                            response.SetContent(JsonHelper.ToJson(result));

                        break;
                    }
                }
            }
            response.Send();
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志消息</param>
        protected void Log(string message)
        {
            if (Logger != null)
                Logger.Log(message);
        }

        #endregion      
    }
}
