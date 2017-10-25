using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Management;
using System.IO;
using System.Reflection;
using Base;

namespace PrintServices
{
    public class Printer
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        #region 打印相关接口

        // type  [1为会员 ,2为员工]
        public Result PrintImage(string imgUrl, string printName)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                //设置边距
                Margins margin = new Margins(0, 0, 0, 0);
                pd.DefaultPageSettings.Margins = margin;
                //横向
                pd.DefaultPageSettings.Landscape = true;
                //启用页边距，获取或设置一个值，该值指示与页关联的图形对象的位置是位于用户指定边距内，还是位于该页可打印区域的左上角。
                pd.OriginAtMargins = true;
                //打印事件设置
                pd.PrintPage += new PrintPageEventHandler((object sender, PrintPageEventArgs e) =>
                {
                    try
                    {
                        var img = LoadImageFromUrl(imgUrl);
                        if (img == null)
                            return;

                        int x = e.MarginBounds.X;
                        int y = e.MarginBounds.Y;
                        int width = e.MarginBounds.Width;
                        int height = e.MarginBounds.Height;

                        Rectangle destRect = new Rectangle(0, 0, width, height);
                        e.Graphics.DrawImage(img, destRect, x, y, img.Width, img.Height, GraphicsUnit.Pixel);
                        img.Dispose();
                    }
                    catch (Exception ex)
                    {
                        this.Logger?.Log(ex.Message);
                    }
                });
                //指定默认打印机的名称 
                printName = GetDefaultPrintName(printName);
                if (string.IsNullOrEmpty(printName))
                    return Result.BuilderStream(0, "未找到可用打印机");

                pd.PrinterSettings.PrinterName = printName;
                if (!pd.PrinterSettings.IsValid)
                    return Result.BuilderStream(0, $"打印机[{printName}]无效");
                
                if (printName.ToLower().Contains("xps"))
                {
                    var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var portName = Path.Combine(directoryName, $"{Environment.TickCount}.xps");
                    var vPrinter = new VirtualPrinter(printName, portName);
                    vPrinter.SetPort(portName);
                }
                pd.Print();

                return Result.BuilderStream(100);
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        // Pos小票打印
        public Result PosPrintHtml(string html, string printName)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                //设置边距
                Margins margin = new Margins(0, 0, 0, 0);
                pd.DefaultPageSettings.Margins = margin;
                //启用页边距，获取或设置一个值，该值指示与页关联的图形对象的位置是位于用户指定边距内，还是位于该页可打印区域的左上角。
                pd.OriginAtMargins = true;
                //打印事件设置
                pd.PrintPage += new PrintPageEventHandler((object sender, PrintPageEventArgs e) =>
                {
                    try
                    {
                        //1、计算打印内容的宽高
                        var bmp = new Bitmap(1, 1);
                        var g = Graphics.FromImage(bmp);
                        var size = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Measure(g, html);
                        g.Dispose();
                        bmp.Dispose();
                        var contentWidth = Convert.ToInt32(Math.Ceiling(size.Width));
                        var contentHeight = Convert.ToInt32(Math.Ceiling(size.Height));
                        //2、打印内容转化为图片
                        bmp = new Bitmap(contentWidth, contentHeight);
                        g = Graphics.FromImage(bmp);
                        TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, new Point(0, 0), size);
#if DEBUG
                        bmp.Save($"{Environment.TickCount}.B.bmp");
                        Logger?.Log($"measure size: width = {size.Width}; height = {size.Height}");
#endif
                        g.Dispose();
                        //3、打印转化好的图片
                        int x = e.MarginBounds.X;
                        int y = e.MarginBounds.Y;
                        Rectangle destRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        e.Graphics.DrawImage(bmp, destRect, x, y, bmp.Width, bmp.Height, GraphicsUnit.Pixel);
                    }
                    catch (Exception ex)
                    {
                        this.Logger?.Log(ex.Message);
                    }
                });
                //指定默认打印机的名称 
                printName = GetDefaultPrintName(printName);
                if (string.IsNullOrEmpty(printName))
                    return Result.BuilderStream(0, "未找到可用打印机");

                pd.PrinterSettings.PrinterName = printName;
                if (!pd.PrinterSettings.IsValid)
                    return Result.BuilderStream(0, $"打印机[{printName}]无效");

                if (printName.ToLower().Contains("xps"))
                {
                    var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var portName = Path.Combine(directoryName, $"{Environment.TickCount}.xps");
                    var vPrinter = new VirtualPrinter(printName, portName);
                    vPrinter.SetPort(portName);
                }
                pd.Print();

                return Result.BuilderStream(100);
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        #endregion

        #region 打印(私有方法)

        //从url加载图片
        private Image LoadImageFromUrl(string url)
        {
            this.Logger?.Log($"ImageUrl={url}");
            try
            {
                var req = WebRequest.Create(url);
                var rep = req.GetResponse();
                using (var stream = rep.GetResponseStream())
                {
                    var img = Image.FromStream(stream);

                    return img;
                }
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
            }

            return null;
        }

        //获取当前电脑上的第一个打印机名称
        private string GetDefaultPrintName(string printName)
        {
            var ms = new ManagementScope(ManagementPath.DefaultPath);
            ms.Connect();

            var sq = new SelectQuery();
            sq.QueryString = @"SELECT Name FROM Win32_Printer";

            var mos = new ManagementObjectSearcher(ms, sq);
            var oObjectCollection = mos.Get();

            foreach (var mo in oObjectCollection)
            {
                if (string.IsNullOrEmpty(printName))
                {
                    if (bool.Parse(mo["Default"].ToString()))
                        return mo["Name"].ToString();
                }
                else if (mo["Name"].ToString().IndexOf(printName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return mo["Name"].ToString();
                }
            }

            return "";
        }

        #endregion
    }
}
