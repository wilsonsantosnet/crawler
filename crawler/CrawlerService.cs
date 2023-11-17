using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Crawler
{
    public class CrawlerService
    {

        private readonly List<Input> invalidLinks = new();
        private readonly List<Input> _validLinks = new();
        private readonly WorkerConfig _config;
        private readonly HttpClient _clientHttp;

        private IWebDriver? _driver;

        public CrawlerService(IOptions<WorkerConfig> config, HttpClient clientHttp)
        {

            _config = config.Value;
            _clientHttp = clientHttp;
        }


        public async Task Run(string[] args)
        {
            var urls = new List<Input> {
                new Input {
                    id = 1,
                    link = "https://wilsonsantosnet.medium.com/artigo-a-61d460aff07c",
                    blackList = new List<string>
                    {
                        "?source=",
                        "signin",
                        "-----"
                    },
                    whiteDomainList = new List<string>
                    {
                        "https://medium.com",
                        "https://wilsonsantosnet.medium.com"
                    },
                    selector = "section",
                    selectorType = Input.SelectorType.tag

                },
            };

            if (args.Length > 0)
            {
                urls = new List<Input>();
                var inputParam = new Input();

                if (args.Where(_ => _.StartsWith("--url=")).Any())
                    inputParam.link = args.Where(_ => _.StartsWith("--url=")).FirstOrDefault().Split("=").LastOrDefault();


                if (args.Where(_ => _.StartsWith("--selector=")).Any())
                    inputParam.selector = args.Where(_ => _.StartsWith("--selector=")).FirstOrDefault().Split("=").LastOrDefault();

                if (args.Where(_ => _.StartsWith("--selectorType=")).Any())
                    inputParam.selectorType = (Input.SelectorType)Convert.ToInt16(args.Where(_ => _.StartsWith("--selectorType=")).FirstOrDefault().Split("=").LastOrDefault());

                if (args.Where(_ => _.StartsWith("--rules")).Any())
                {
                    inputParam.blackList.AddRange(_config.BlackList.Split(";"));
                    inputParam.whiteDomainList.AddRange(_config.WhiteDomainList.Split(";"));
                }

                urls.Add(inputParam);



            }

            var urlJsonParm = JsonSerializer.Serialize(urls);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(urlJsonParm);
            Console.ResetColor();



            // Verifica se a pasta de saída existe, senão a cria
            Directory.CreateDirectory(_config.OutputFolderPathHtml);
            Directory.CreateDirectory(_config.OutputFolderPathPDF);

            var executeConvertMainUrls = false;
            var executeExtractLinks = false;
            var executeProcessFilesLinks = false;

            if (args.Length > 0)
            {
                Console.WriteLine("Lendo parâmetros");

                if (args.Where(_ => _ == "--p1").Any())
                    executeConvertMainUrls = true;

                if (args.Where(_ => _ == "--p2").Any())
                    executeExtractLinks = true;

                if (args.Where(_ => _ == "--p3").Any())
                    executeProcessFilesLinks = true;
            }
            else
            {
                Console.WriteLine("sem parâmetros");

                executeConvertMainUrls = true;
                executeExtractLinks = true;
                executeProcessFilesLinks = true;
            }


            if (executeConvertMainUrls) await ConvertMainUrls(urls);

            if (executeExtractLinks) await ExtractLinks(urls);

            if (executeProcessFilesLinks) await ProcessFilesLinks();

        }

        private void SaveAllLinks()
        {
            var json = JsonSerializer.Serialize(_validLinks);
            var filePath = "all-links.txt";
            File.WriteAllText(filePath, json);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(_validLinks.Count + " links extraídos e salvos em " + filePath);
            Console.ResetColor();

        }

        private void SaveLinksDataverse()
        {
            var links = _validLinks.Select(_ => new { _.link });

            _clientHttp.BaseAddress = new Uri($"{_config.Paurl}");
            var responseGroup = _clientHttp.PostAsync("", new StringContent(JsonSerializer.Serialize(links), Encoding.UTF8, "application/json")).Result;
            var statusCode = responseGroup.StatusCode;
            var dataMyApi = responseGroup.Content.ReadAsStringAsync().Result;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(_validLinks.Count + " links salvos em " + _config.Paurl);
            Console.ResetColor();
        }

        private async Task ConvertMainUrls(List<Input> inputs)
        {
            foreach (var input in inputs.Where(_ => _.iteration))
            {
                await ConvertContentHtml(_config.OutputFolderPathHtml, _config.OutputFolderPathPDF, input, disableIteration: false);
            }

            //Parallel.ForEach(inputs.Where(_ => !_.iteration), async (input) =>
            foreach (var input in inputs.Where(_ => !_.iteration))
            {
                await ConvertContentHtml(_config.OutputFolderPathHtml, _config.OutputFolderPathPDF, input, disableIteration: false);
            };
        }

        private async Task ExtractLinks(List<Input> inputs)
        {

            SaveLinks("main.txt", inputs);

            foreach (var input in inputs.Where(_ => _.iteration))
            {
                await GetLink(input);
            }

            //Parallel.ForEach(inputs.Where(_ => !_.iteration), async (input) =>
            foreach (var input in inputs.Where(_ => !_.iteration))
            {
                await GetLink(input);
            };

            SaveAllLinks();

            SaveLinksDataverse();
        }

        private async Task ProcessFilesLinks()
        {
            string[] txtFiles = Directory.GetFiles(".", "*.txt");

            if (!txtFiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Nehum arquivo de link encontrado em " + AppContext.BaseDirectory);
                Console.ResetColor();
            }

            //Parallel.ForEach(txtFiles, async (file) =>
            foreach (var file in txtFiles)
            {
                await ProcessFile(file);
            }
        }

        private async Task ProcessFile(string inputFilePath)
        {

            try
            {
                var contentFile = File.ReadAllText(inputFilePath);
                var inputs = System.Text.Json.JsonSerializer.Deserialize<List<Input>>(contentFile);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Processando " + inputs.Count() + " links do aquivo " + inputFilePath);
                Console.ResetColor();



                //Parallel.ForEach(inputs, async (input) =>
                foreach (Input input in inputs)
                {
                    await ConvertContentHtml(_config.OutputFolderPathHtml, _config.OutputFolderPathPDF, input, disableIteration: true);
                };

                if (invalidLinks.Any())
                {
                    var errorFile = inputFilePath.Replace(".txt", ".err");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Gerado " + invalidLinks.Count() + " links do aquivo " + errorFile);
                    Console.ResetColor();

                    // Salve o HTML no arquivo
                    File.WriteAllText(errorFile, string.Join(Environment.NewLine, invalidLinks));
                    invalidLinks.Clear();
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ocorreu um erro: " + ex.Message);
                Console.ResetColor();
            }
        }

        private async Task ConvertContentHtml(string outputFolderPathHtml, string outputFolderPathPDF, Input input, bool disableIteration)
        {

            try
            {

                var content = await GetHtmlFromUrlBySelenium(input, disableIteration);

                var fileNameHtml = Path.Combine(outputFolderPathHtml, input.prefixo + "-" + input.id + ".html");
                SaveHtml(input, content, fileNameHtml);

                var fileNamePDF = Path.Combine(outputFolderPathPDF, input.prefixo + "-" + input.id + ".pdf");
                SavePdfFromHtmlWkhtmltopdf(input, fileNameHtml, Path.GetFullPath(fileNamePDF));


            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var error = $"Ocorreu um erro no link {input.link}: " + ex.Message;
                Console.WriteLine(error);
                Console.ResetColor();

                invalidLinks.Add(new Input
                {

                    id = invalidLinks.Count(),
                    link = input.link,
                    selector = input.selector,
                    iteration = input.iteration,
                    selectorType = input.selectorType,
                    waitTimeout = input.waitTimeout,
                    error = error,
                    sourceId = input.id,
                });
            }
        }


        private void SaveHtml(Input input, string content, string fileNameHtml)
        {
            // Salve o HTML no arquivo
            File.WriteAllTextAsync(fileNameHtml, content);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("HTML " + input.id + " da página " + input.link + " salvo em " + fileNameHtml);
            Console.ResetColor();
        }

        private async Task GetLink(Input input)
        {

            string filePath = GetValidFileName(input.link + ".txt");

            try
            {
                //var httpClient = new HttpClient();
                //var html = await httpClient.GetStringAsync(input.link);

                var html = await GetHtmlFromUrlBySelenium(input, false);

                var uri = new Uri(input.link);
                var baseDomain = $"{uri.Scheme}://{uri.Host}";

                var links = GetLinksFromHtml(html, input, baseDomain, filePath);

                if (!_validLinks.Where(_ => _.link == input.link).Any())
                    _validLinks.Add(input);

                if (links.Count > 0)
                {
                    SaveLinks(filePath, links);

                    foreach (var link in links)
                    {
                        if (!_validLinks.Where(_ => _.link == link.link).Any())
                        {
                            _validLinks.Add(link);
                            await GetLink(link);
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Nenhum link encontrado na página " + input.link);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ocorreu um erro: para acessar " + input.link + ": " + ex.Message);
                Console.ResetColor();
            }
        }

        private void SaveLinks(string filePath, List<Input> links)
        {
            var json = JsonSerializer.Serialize(links);
            File.WriteAllText(filePath, json);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(links.Count + " links extraídos e salvos em " + filePath);
            Console.ResetColor();
        }

        List<Input> GetLinksFromHtml(string html, Input input, string baseDomain, string filePath)
        {
            List<Input> links = new List<Input>();

            // Expressão regular para encontrar links
            string pattern = @"<a\s+(?:[^>]*?\s+)?href\s*=\s*[""']([^""']*)[""']";
            var id = 0;
            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
            {
                try
                {
                    var linkComplete = match.Groups[1].Value;

                    if (!linkComplete.Contains("http"))
                        linkComplete = baseDomain + linkComplete;

                    if (input.blackList.Any(item => linkComplete.Contains(item))) continue;


                    if (input.sameDomain)
                        if (!input.whiteDomainList.Any(item => linkComplete.Contains(item))) continue;


                    if (linkComplete == input.link) continue;

                    if (linkComplete.Split('#').FirstOrDefault() == input.link.Split('#').FirstOrDefault()) continue;

                    if (_validLinks.Where(_ => _.link == linkComplete).Any()) continue;



                    if (!links.Where(_ => _.link == linkComplete).Any())
                    {
                        links.Add(new Input
                        {
                            id = id++,
                            link = linkComplete,
                            selector = input.selector,
                            iteration = false,
                            selectorType = input.selectorType,
                            waitTimeout = input.waitTimeout,
                            prefixo = "sub-" + filePath,
                            sourceId = input.id,
                            blackList = input.blackList,
                            sameDomain = input.sameDomain,
                            whiteDomainList = input.whiteDomainList
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Ocorreu um erro no link " + input.link + " : " + ex.Message);
                    Console.ResetColor();

                }

            }

            return links;
        }

        async Task<string> GetHtmlFromUrlBySelenium(Input input, bool disableIteration)
        {
            var pageSource = "";

            IWebDriver driver = GetInstaceSeleniumWebDriver();

            try
            {


                // Navegar para a página da web desejada
                driver.Navigate().GoToUrl(input.link);

                if (input.iteration && !disableIteration)
                {
                    while (true)
                    {
                        Console.WriteLine("Quando desejar seguir digite S");
                        var continueprocess = Console.ReadLine();
                        if (continueprocess.ToUpper() == "S")
                            break;
                    }

                }
                else
                {
                    Thread.Sleep(input.waitTimeout);
                }


                if (input.selector != null)
                {
                    // Obter o HTML da página
                    //string pageSource = driver.PageSource;
                    if (input.selectorType == Input.SelectorType.id)
                    {
                        IWebElement elemento = driver.FindElement(By.Id(input.selector));
                        //string conteudo = elemento.Text;
                        pageSource = elemento.GetAttribute("innerHTML");
                    }

                    if (input.selectorType == Input.SelectorType.tag)
                    {
                        IWebElement elemento = driver.FindElement(By.TagName(input.selector));
                        //string conteudo = elemento.Text;
                        pageSource = elemento.GetAttribute("innerHTML");
                    }
                }
                else
                {
                    pageSource = driver.PageSource;
                }

                // Fechar o navegador
                //driver.Quit();


            }
            catch (Exception ex)
            {
                // Fechar o navegador
                //driver.Quit();
                throw ex;
            }

            return pageSource;

        }


        private IWebDriver GetInstaceSeleniumWebDriver()
        {
            // Especifique o caminho para o ChromeDriver.exe
            //string driverPath = "C:\\chromedriver_win32\\chromedriver.exe";
            //string driverPath = "C:\\chromedriver-win64\\";
            string driverPath = _config.DriverPath;

            // Configurar as opções do Chrome
            var chromeOptions = new ChromeOptions();
            //chromeOptions.AddArgument("--headless"); // Opcional: Executar o Chrome em modo headless (sem interface gráfica)
            //chromeOptions.AddArgument("--user-data-dir=C:\\Users\\wdossantos\\AppData\\Local\\Google\\Chrome\\User Data\\"); // Opcional: Executar o Chrome em modo headless (sem interface gráfica)
            //chromeOptions.AddArgument("--user-data-dir=C:\\Users\\AB670412\\AppData\\Local\\Google\\Chrome\\User Data\\"); // Opcional: Executar o Chrome em modo headless (sem interface gráfica)
            //chromeOptions.AddArgument(_config.AddArgument); // Opcional: Executar o Chrome em modo headless (sem interface gráfica)
            //chromeOptions.AddArgument("--disable-logging");
            //chromeOptions.AddArgument("--log-level=3");
            //chromeOptions.AddArgument("--output=/dev/null");

            var arguments = _config.AddArgument.Split(" ");
            foreach (var item in arguments)
            {
                chromeOptions.AddArgument(item);
            }

            // Inicializar o driver do Chrome
            if (_driver == null)
                _driver = new ChromeDriver(driverPath, chromeOptions);

            return _driver;
        }

        void SavePdfFromHtmlWkhtmltopdf(Input input, string htmlFilePath, string pdfFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _config.Pdfexe;
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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PDF " + input.id + " da página " + input.link + " salvo em " + pdfFilePath);
                Console.ResetColor();

            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var error = "PDF da página " + input.link + " não encontrado em " + pdfFilePath;
                Console.WriteLine(error);
                Console.ResetColor();
                throw new Exception(error);
            }
        }

        static string GetValidFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName.Replace(';', '_');
        }

    }
}
