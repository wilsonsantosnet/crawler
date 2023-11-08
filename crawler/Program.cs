using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


class Program
{
    static readonly string outputFolderPathHtml = "html_outputs";
    static readonly string outputFolderPathPDF = "pdf_outputs";
    static readonly List<string> invalidLinks = new();

    public class Input {

        public string link { get; set; }
        public string cssSeletor { get; set; }
    }
    static async Task Main(string[] args)
    {
        var urls = new List<Input> {
            new Input {
                link = "https://wilsonsantosnet.medium.com/apim-d537b36ef5d0",
                cssSeletor = "section"
            }
           
        };

        // Verifica se a pasta de saída existe, senão a cria
        Directory.CreateDirectory(outputFolderPathHtml);
        Directory.CreateDirectory(outputFolderPathPDF);


        await ConvertUrls(urls);

        await ExtractLinks(urls);

        //await ProcessFilesLinks();

        Console.Read();

    }

    private static async Task ConvertUrls(List<Input> urls)
    {
        Parallel.ForEach(urls, async (url) =>
        //foreach (var url in urls)
        {
            await ConvertContentHtml(outputFolderPathHtml, outputFolderPathPDF, url.link, url.cssSeletor);
        });
    }

    private static async Task ExtractLinks(List<Input> urls)
    {
        Parallel.ForEach(urls, async (url) =>
        //foreach (var url in urls)
        {
            await GetLink(url.link, url.cssSeletor);
        });
    }

    private static async Task ProcessFilesLinks()
    {
        string[] txtFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.txt");
        Parallel.ForEach(txtFiles, async (file) =>
        //foreach (var file in txtFiles)
        {
            await ProcessFile(file);
        });
    }

    private static async Task ProcessFile(string inputFilePath)
    {

        try
        {
            var contentFile = File.ReadAllText(inputFilePath);
            var urls = System.Text.Json.JsonSerializer.Deserialize<List<Input>>(contentFile);

            Parallel.ForEach(urls, async (url) =>
            //foreach (Input url in urls)
            {
                await ConvertContentHtml(outputFolderPathHtml, outputFolderPathPDF, url.link, url.cssSeletor);
            });

            // Salve o HTML no arquivo
            File.WriteAllText(inputFilePath.Replace(".txt", ".err"), string.Join(Environment.NewLine, invalidLinks));
            invalidLinks.Clear();

        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro: " + ex.Message);
        }
    }

    private static async Task ConvertContentHtml(string outputFolderPathHtml, string outputFolderPathPDF, string link, string cssSelector)
    {

        try
        {
            var content = "";
            if (!String.IsNullOrEmpty(cssSelector))
                content = await GetHtmlFromUrlByScraping(link, cssSelector);    
            else
                content = await GetHtmlFromUrlByHttp(link, cssSelector);

            var fileNameHtml = Path.Combine(outputFolderPathHtml, GetValidFileName(link) + ".html");
            SaveHtml(link, content, fileNameHtml);

            var fileNamePDF = Path.Combine(outputFolderPathPDF, GetValidFileName(link) + ".pdf");
            
            SavePdfFromHtmlWkhtmltopdf(fileNameHtml, Path.GetFullPath(fileNamePDF));


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro no link {link}: " + ex.Message);
            invalidLinks.Add(link);
        }
    }


    private static void SaveHtml(string link, string content, string fileNameHtml)
    {
        // Salve o HTML no arquivo
        File.WriteAllTextAsync(fileNameHtml, content);
        Console.WriteLine("HTML da página " + link + " salvo em " + fileNameHtml);
    }

    private static async Task GetLink(string url, string cssSelector)
    {

        string filePath = GetValidFileName(url + ".txt");

        try
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var uri = new Uri(url);
            var baseDomain = $"{uri.Scheme}://{uri.Host}";

            var links = GetLinksFromHtml(html, cssSelector, baseDomain);

            if (links.Count > 0)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(links);

                File.WriteAllText(filePath, json);

                Console.WriteLine("Links extraídos e salvos em " + filePath);

                await ProcessFile(filePath);
            }
            else
            {
                Console.WriteLine("Nenhum link encontrado na página.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro: " + ex.Message);
        }
    }

    static List<dynamic> GetLinksFromHtml(string html, string cssSelector, string baseDomain)
    {
        List<dynamic> links = new List<dynamic>();

        // Expressão regular para encontrar links
        string pattern = @"<a\s+(?:[^>]*?\s+)?href\s*=\s*[""']([^""']*)[""']";

        foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
        {
            var linkComplete = match.Groups[1].Value;

            if (!linkComplete.Contains("http"))
                linkComplete = baseDomain + linkComplete;

            links.Add(new
            {
                link = linkComplete,
                cssSeletor = cssSelector

            });
        }

        return links;
    }

    static async Task<string> GetHtmlFromUrlByScraping(string url, string cssSelector)
    {

        ScrapingBrowser browser = new ScrapingBrowser();
        browser.Encoding = Encoding.UTF8;
        WebPage page = browser.NavigateToPage(new Uri(url));
        //return page.Html.InnerHtml;

        HtmlNode[] nodes = page.Html.CssSelect(cssSelector).ToArray();
        StringBuilder sb = new StringBuilder();
        sb.Append("<html><head><meta charset=\"UTF-8\"></head><body>");
        foreach (HtmlNode node in nodes)
        {
            sb.Append(node.InnerHtml);
        }
        sb.Append("</body></html>");
        return sb.ToString();


    }

    static async Task<string> GetHtmlFromUrlByHttp(string url, string cssSelector)
    {

        using (var httpClient = new HttpClient())
        {
            return await httpClient.GetStringAsync(url);
        }
    }
   
    static void SavePdfFromHtmlWkhtmltopdf(string htmlFilePath, string pdfFilePath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "C:\\Program Files\\wkhtmltopdf\\bin\\wkhtmltopdf.exe";
        startInfo.Arguments = $"{htmlFilePath} {pdfFilePath}";
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        if (File.Exists(pdfFilePath))
        {
            Console.WriteLine("PDF file created successfully!");
        }
        else
        {
            Console.WriteLine("PDF file creation failed!");
        }
    }

    static string GetValidFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

}
