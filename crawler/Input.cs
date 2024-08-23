namespace Crawler
{
    public class Input
    {
        public Input()
        {
            this.prefixo = "main";
            this.blackList = new();
            this.whiteDomainList = new();
            this.selectorType = SelectorType.tag;
            this.cromeDriverTimeout = 240;
            this.waitTimeout = 6000;
            this.waitForExit = 60000;
            this.url = string.Empty;



        }

        public enum SelectorType { id, tag, classname }

        public int id { get; set; }
        public SelectorType selectorType { get; set; }
        public string url { get; set; }
        public string selector { get; set; }

        public bool iteration { get; set; }

        public string error { get; set; }

        public int sourceId { get; set; }

        public string prefixo { get; set; }

        public List<string> blackList { get; set; }

        public List<string> whiteDomainList { get; set; }

        public string filenameHTML { get { return this.prefixo + "-" + this.id + "-" + (this.url.Length > 100 ? this.GetValidFileName(this.url[..150]) : this.GetValidFileName(this.url)) + ".html"; } }

        public string filenamePDF { get { return this.prefixo + "-" + this.id + "-" + (this.url.Length > 100 ? this.GetValidFileName(this.url[..150]) : this.GetValidFileName(this.url)) + ".pdf"; } }

        public int cromeDriverTimeout { get; set; }

        public int waitTimeout { get; set; }

        public int waitForExit { get; set; }

        public bool savePartial { get; set; }

        public string baseDomain { get; set; }

        public string GetBaseDomain()
        {
            if (string.IsNullOrEmpty(this.baseDomain))
            {
                var uri = new Uri(this.url);
                var baseDomain = $"{uri.Scheme}://{uri.Host}";
                return baseDomain;
            }
            return this.baseDomain;
        }

        public string GetValidFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName.Replace(';', '_');
        }
    }

}
