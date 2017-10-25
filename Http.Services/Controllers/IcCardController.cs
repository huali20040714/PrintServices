using Base;
using HTTPServerLib;

namespace Http.Services.Core
{
    public class IcCardController : Controller
    {
        // 设备初始化
        public Result Init()
        {
            //var icCard = new IcCard() { Logger = Logger };
            //return icCard.Init();
            return Program.IcCard.Init();
        }

        // 读取IcCard机器码(唯一)
        public Result GetIcCardMachineId()
        {
            //var icCard = new IcCard() { Logger = Logger };

            return Program.IcCard.GetIcCardMachineId();
        }

        // 读取IcCard信息
        public Result GetIcCard()
        {
            var randomSecret = Request.Params["RandomSecret"];
            //var icCard = new IcCard() { Logger = Logger };

            return Program.IcCard.GetIcCard(randomSecret);
        }

        // 写入IcCard数据, 只写入第一扇区
        public Result WriteIcCard()
        {
            var randomSecret = Request.Params["RandomSecret"];
            var data = Request.Params["Data"];
            //var icCard = new IcCard() { Logger = Logger };

            return Program.IcCard.WriteIcCard(randomSecret, data);
        }

        // 修改KeyA
        public Result ChangeKeyIcCard()
        {
            var randomSecret = Request.Params["RandomSecret"];
            var newRandomSecret = Request.Params["NewRandomSecret"];
            //var icCard = new IcCard() { Logger = Logger };

            return Program.IcCard.ChangeKeyIcCard(randomSecret, newRandomSecret);
        }
    }
}
