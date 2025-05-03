using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RedisServiceCtrl
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceCtrl()
            };
            ServiceBase.Run(ServicesToRun);

            //var service = new ServiceCtrl();
            //service.myStart();
            //Console.WriteLine("按任意键停止服务...");
            //Console.ReadKey();
            //service.myStop();
        }
    }
}
