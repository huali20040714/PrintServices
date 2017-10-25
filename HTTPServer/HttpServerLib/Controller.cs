using Base;

namespace HTTPServerLib
{
    public class Controller
    {
        public ILogger Logger { get; set; }
        public HttpRequest Request { get; set; }
        public HttpResponse Response { get; set; }
    }
}
