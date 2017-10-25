using System;
using System.IO;
using Topshelf;
using HTTPServerLib;
using Http.Services.Core;
using Base;
using IcReadCardServices;

namespace Http.Services
{
    class Program
    {
        public static Logger Logger { get; } = new Logger();
        public static IcCard IcCard { get; } = new IcCard() { Logger = Logger };

        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            //处理非UI线程异常   
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //初始化读卡服务
            IcCard.Init();

            // 安装或运行服务，由运行时提交的参数决定
            RunServices();
        }

        // 安装或运行服务，由运行时提交的参数决定
        static void RunServices()
        {
            HostFactory.Run(x =>                                                    //1.我们用HostFactory.Run来设置一个宿主主机。我们初始化一个新的lambda表达式X，来显示这个宿主主机的全部配置。
            {
                x.UseLog4Net("log4net.config");                                     //2.加载日志配置
                x.Service<HttpServer>(s =>                                        //3.告诉Topshelf ，有一个类型为“towncrier服务”,通过定义的lambda 表达式的方式，配置相关的参数。
                {
                    //4.告诉Topshelf如何创建这个服务的实例，目前的方式是通过new 的方式，
                    //  但是也可以通过Ioc 容器的方式：getInstance<towncrier>()。
                    s.ConstructUsing(name =>
                    {
                        return new HttpServer("127.0.0.1", 9191) { Logger = Logger };
                    });

                    s.WhenStarted(tc => tc.Start());                                //5.开始 Topshelf 服务。
                    s.WhenStopped(tc => tc.Stop());                                 //6.停止 Topshelf 服务。                    

                    s.BeforeStartingService(_ => Logger.Log("BeforeStart"));
                    s.BeforeStoppingService(_ => Logger.Log("BeforeStop"));
                });

                x.SetStartTimeout(TimeSpan.FromSeconds(10));
                x.SetStopTimeout(TimeSpan.FromSeconds(10));

                x.RunAsLocalSystem();                                               //7.这里使用RunAsLocalSystem() 的方式运行，也可以使用命令行(RunAsPrompt())等方式运行。
                //x.RunAsNetworkService();

                x.SetDescription("IcCard、XiMaCard Print and Pos Print Services");             //8.设置A.Http.Services服务在服务监控中的描述。
                x.SetDisplayName("A.Http.Services");                                //9.设置A.Http.Services服务在服务监控中的显示名字。
                x.SetServiceName("A.Http.Services");                                //10.设置A.Http.Services服务在服务监控中的服务名字。                

                x.OnException((exception) =>
                {
                    Logger.Log("Exception thrown - " + exception.Message);
                });
            });
        }

        // 全局异常错误捕获
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex == null)
                Logger.Log($"出现未处理的异常:{e}");
            else
                Logger.Log(ex.StackTrace);
        }
    }
}
