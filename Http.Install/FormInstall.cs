using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;

namespace Http.Install
{
    public partial class FormInstall : Form
    {
        private string serviceExeName = string.Empty;
        private string serviceName = string.Empty;

        public FormInstall()
        {
            InitializeComponent();
        }

        private void FormInstall_Load(object sender, System.EventArgs e)
        {
            this.serviceExeName = ConfigurationManager.AppSettings["ServiceExeName"];
            this.serviceName = ConfigurationManager.AppSettings["ServiceName"];
        }

        // 安装服务
        private void btnInstall_Click(object sender, System.EventArgs e)
        {
            var service = FindService(this.serviceName);
            if (service != null)
            {
                this.txtLog.AppendText($"【{this.serviceName}】服务已存在{Environment.NewLine}");
                return;
            }
            StartProcess(this.serviceExeName, "install start");
        }

        // 卸载服务
        private void btnUninstall_Click(object sender, System.EventArgs e)
        {
            var service = FindService(this.serviceName);
            if (service == null)
            {
                this.txtLog.AppendText($"【{this.serviceName}】服务未找到{Environment.NewLine}");
                return;
            }
            var killName = this.serviceExeName;
            var fileInfo = new FileInfo(this.serviceExeName);
            if (fileInfo.Exists)
            {
                killName = fileInfo.Name.Replace(fileInfo.Extension, "");
            }
            KillProcessByName(killName);
            StartProcess(this.serviceExeName, "uninstall");
        }

        // 启动服务
        private void btnStart_Click(object sender, EventArgs e)
        {
            var service = FindService(this.serviceName);
            if (service != null)
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    this.txtLog.AppendText($"【{this.serviceName}】服务启动中...{Environment.NewLine}");
                }
                else
                {
                    this.txtLog.AppendText($"【{this.serviceName}】服务启动闭{Environment.NewLine}");
                }
            }
            else
            {
                this.txtLog.AppendText($"【{this.serviceName}】服务未找到{Environment.NewLine}");
            }
        }

        // 停止服务
        private void btnStop_Click(object sender, EventArgs e)
        {
            var service = FindService(this.serviceName);
            if (service != null)
            {
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    this.txtLog.AppendText($"【{this.serviceName}】服务关闭中...{Environment.NewLine}");
                }
                else
                {
                    this.txtLog.AppendText($"【{this.serviceName}】服务已关闭{Environment.NewLine}");
                }
            }
            else
            {
                this.txtLog.AppendText($"【{this.serviceName}】服务未找到{Environment.NewLine}");
            }
        }

        // 查看服务状态
        private void btnShowStatus_Click(object sender, System.EventArgs e)
        {
            var service = FindService(this.serviceName);
            if (service == null)
            {
                this.txtLog.AppendText($"【{this.serviceName}】服务未找到{Environment.NewLine}");
            }
            else
            {
                this.txtLog.AppendText($"服务【{this.serviceName}】{service.Status}{Environment.NewLine}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 检测服务是否存在
        /// </summary>
        /// <param name="serviceName">服务名</param>
        /// <returns></returns>
        private ServiceController FindService(string serviceName)
        {
            var services = ServiceController.GetServices();
            foreach (var service in services)
            {
                if (service.ServiceName.Equals(serviceName, System.StringComparison.OrdinalIgnoreCase))
                    return service;
            }

            return null;
        }

        /// <summary>
        /// 根据文件执行名关闭程序
        /// </summary>
        /// <param name="name"></param>
        public void KillProcessByName(string name)
        {
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        this.txtLog.AppendText($"开始结束：{p.ProcessName}{Environment.NewLine}");
                        p.Kill();
                        p.WaitForExit();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 启动其他服务
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="args"></param>
        private void StartProcess(string exe, params string[] args)
        {
            var param = string.Join(" ", args);
            this.txtLog.AppendText($"开始执行：{exe} {param}{Environment.NewLine}");

            Process process = new Process();
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = param;
            process.StartInfo.Verb = "runas";                              //设置启动动作,确保以管理员身份运行
            process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            process.Start();
            process.WaitForExit();

            string ret = process.StandardOutput.ReadToEnd();
            this.txtLog.AppendText($"开始结果：{ret}{Environment.NewLine}");
        }

        #endregion
    }
}
