namespace Crawler
{
    public class Input
    {
        public Input()
        {
            this.waitTimeout = 3000;
            this.prefixo = "main";
            this.blackList = new();
            this.whiteDomainList = new();
            this.sameDomain = true;
            this.selectorType = SelectorType.tag;
        }

        public enum SelectorType { id, tag }

        public int id { get; set; }
        public SelectorType selectorType { get; set; }
        public string link { get; set; }
        public string selector { get; set; }
        public bool iteration { get; set; }
        public int waitTimeout { get; set; }

        public string error { get; set; }

        public int sourceId { get; set; }

        public string prefixo { get; set; }

        public bool sameDomain { get; set; }

        public List<string> blackList { get; set; }

        public List<string> whiteDomainList { get; set; }

    }

}
