using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETLSoftware.Utility
{
    public class Logger
    {
        private static string directoryPath = AppDomain.CurrentDomain.BaseDirectory + "logs\\";

        string pathFile { get; set; }

        public Logger()
        {
            pathFile = directoryPath + DateTime.Today.ToString("yyyy_MM_dd") + ".txt";
        }

        public void WriteLog(string content)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.AppendAllLines(pathFile, new List<string> { content });
        }

    }
}
