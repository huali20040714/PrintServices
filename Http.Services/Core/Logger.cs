using Base;
using HTTPServerLib;
using Topshelf.Logging;

namespace Http.Services.Core
{
    public class Logger : ILogger
    {
        static readonly LogWriter _log = HostLogger.Get<HttpServer>();

        public void Log(string message)
        {
            _log.Info(message);
        }
    }
}
