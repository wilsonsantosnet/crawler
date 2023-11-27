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
            this.PdfMethod = 2;
        }
        public string OutputFolderPathHtml { get; set; }
        public string OutputFolderPathPDF { get; set; }
        public string Paurl { get; set; }
        public string DriverPath { get; set; }
        public string ChromePath { get; set; }
        
        public string AddArgument { get; set; }
        public string AddArgument2 { get; set; }
        public string Pdfexe { get; set; }
        public string PdfArguments { get; set; }
        public string BlackList { get; set; }

        public string WhiteDomainList { get; set; }

        public int PdfMethod { get; set; }


    }
}
