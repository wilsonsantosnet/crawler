using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    public class WorkerConfig
    {
        public enum PdfMethodType { WkHtmltoPdf, Chrome, ChromeDrive }

        public WorkerConfig()
        {
            this.PdfMethod = PdfMethodType.ChromeDrive;
            this.ReplaceHtml = new Dictionary<string, string>
            {
                //{ "<img src=\"../","<img src=\"http://seuportal.vivo.com.br/portal_unico/midias/hotsites/" },
                { "<img src=\"/VivoStart/","<img src=\"https://vivo.my.site.com/VivoStart/" }
            };
            this.TryCount = 0;
            this.OutputFolderPathHtml = "html_outputs";
            this.OutputFolderPathHtmlImages = "images";
        }
        public string OutputFolderPathHtmlImages { get; set; }
        public string OutputFolderPathHtml { get; set; }
        public string OutputFolderPathPDF { get; set; }

        public string OutputFolderPathLinks { get; set; }
        public string Paurl { get; set; }
        public string DriverPath { get; set; }
        public string ChromePath { get; set; }

        public string AddArgument { get; set; }
        public string AddArgument2 { get; set; }
        public string Pdfexe { get; set; }
        public string Proxy { get; set; }

        public string PdfArguments { get; set; }
        public string BlackList { get; set; }

        public string WhiteDomainList { get; set; }

        public PdfMethodType PdfMethod { get; set; }


        public Dictionary<string, string> ReplaceHtml { get; set; }
        public int TryCount { get; set; }
    }
}
