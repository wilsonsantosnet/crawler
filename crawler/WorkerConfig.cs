using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    public class WorkerConfig
    {
        

        public WorkerConfig()
        {
            //this.OutputFolderPathHtml = "html_outputs";
            //this.OutputFolderPathPDF = "pdf_outputs";
            //this.Paurl = "https://prod-01.brazilsouth.logic.azure.com:443/workflows/02c7be82dce240c784d3961229273270/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=JEPy_E98lkc5zvSylDiuipZgsYtCSfgAKASt-o_CS-o";
            //this.DriverPath = "C:\\chromedriver_win32\\chromedriver.exe";
            //this.AddArgument = "--user-data-dir=C:\\Users\\wdossantos\\AppData\\Local\\Google\\Chrome\\User Data\\";
            //this.Pdfexe = "C:\\Program Files\\wkhtmltopdf\\bin\\wkhtmltopdf.exe";
        }
        public string OutputFolderPathHtml { get; set; }
        public string OutputFolderPathPDF { get; set; }
        public string Paurl { get; set; }
        public string DriverPath { get; set; }
        public string AddArgument { get; set; }
        public string Pdfexe { get; set; }

        public string BlackList { get; set; }

        public string WhiteDomainList { get; set; }

        
    }
}
