using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Crawler
{
    public class CrawlerService
    {

        private readonly HashSet<Input> _invalidLinks = new();
        private readonly HashSet<Input> _validLinks = new();
        private readonly WorkerConfig _config;
        private readonly HttpClient _clientHttpToken;
        private readonly HttpClient _clientHttp;

        private ChromeDriver? _driver;

        public CrawlerService(IOptions<WorkerConfig> config, IHttpClientFactory clientHttp)
        {

            _config = config.Value;
            _clientHttpToken = clientHttp.CreateClient("crawlerClient");
            _clientHttp = clientHttp.CreateClient("crawlerClient");
        }


        public async Task Run(string[] args)
        {
            var urls = new List<Input> {
                //new Input {
                //    id = 1,
                //    link = "http://seuportal.vivo.com.br/nosso_portal/movel/movel_pf/",
                //    iteration = true
                //},
                //new Input {
                //    id = 1,
                //    link = "http://vivo.my.salesforce.com",
                //    iteration = true
                //},
                //new Input {
                //    id = 1,
                //    link = "https://vivo.my.site.com/VivoStart/s/article/IT-Como-fazer-envio-de-protocolo-no-360",
                //    iteration = true,
                //}
                //new Input {
                //    id = 1,
                //    link = "https://atento-vivovpe.plusoftomni.com.br/?m=menu.main.callcenter&dest=%2Fforms%2Fatentovivo.plugintabs.main.forms.signon%2F\"",
                //    iteration = true,
                //}
                new Input {
                    id = 1,
                    link = "https://atento-vivo.inpaas.com/api/ckb-portal-online/portal-vivo/pt/inicio",
                    iteration = true,
                }
            };


            // Verifica se a pasta de saída existe, senão a cria
            Directory.CreateDirectory(Path.Combine(_config.OutputFolderPathHtml, _config.OutputFolderPathHtmlImages));
            Directory.CreateDirectory(_config.OutputFolderPathHtml);
            Directory.CreateDirectory(_config.OutputFolderPathPDF);
            Directory.CreateDirectory(_config.OutputFolderPathLinks);

            var executeConvertMainUrls = false;
            var executeExtractLinks = false;
            var executeProcessFilesLinks = false;
            var executeSendFilesOut = false;

            if (args.Length > 0)
            {
                Console.WriteLine("Lendo parâmetros");

                if (args.Where(_ => _ == "--p1").Any())
                    executeConvertMainUrls = true;

                if (args.Where(_ => _ == "--p2").Any())
                    executeExtractLinks = true;

                if (args.Where(_ => _ == "--p3").Any())
                    executeProcessFilesLinks = true;

                if (args.Where(_ => _ == "--p4").Any())
                    executeSendFilesOut = true;

                urls = ExtraParameters(args, urls);
            }
            else
            {
                Console.WriteLine("sem parâmetros");

                executeConvertMainUrls = false;
                executeExtractLinks = false;
                executeProcessFilesLinks = false;
                executeSendFilesOut = true;
            }

            var urlJsonParm = JsonSerializer.Serialize(urls);
            LogYellow(urlJsonParm);


            if (executeConvertMainUrls) await ConvertMainUrls(urls);

            if (executeExtractLinks) await ExtractLinks(urls);

            if (executeProcessFilesLinks) await ProcessFilesLinks();

            if (executeSendFilesOut) await SendFilesOut();





            if (_driver != null)
                _driver.Quit();

            LogYellow("Process End");

        }

        private List<Input> ExtraParameters(string[] args, List<Input> urls)
        {
            if (args.Length > 0)
            {
                urls = new List<Input>();
                var inputParam = new Input();


                if (args.Where(_ => _.StartsWith("--configlists")).Any())
                {
                    if (!string.IsNullOrEmpty(_config.BlackList))
                        inputParam.blackList.AddRange(_config.BlackList.Split(";"));
                    if (!string.IsNullOrEmpty(_config.WhiteDomainList))
                        inputParam.whiteDomainList.AddRange(_config.WhiteDomainList.Split(";"));
                }


                if (args.Where(_ => _.StartsWith("--configbasedomain")).Any())
                {
                    inputParam.baseDomain = _config.BaseDomain;
                }


                if (args.Where(_ => _.StartsWith("--url=")).Any())
                    inputParam.link = args.Where(_ => _.StartsWith("--url=")).FirstOrDefault().Split("=").LastOrDefault();


                if (args.Where(_ => _.StartsWith("--selector=")).Any())
                    inputParam.selector = args.Where(_ => _.StartsWith("--selector=")).FirstOrDefault().Split("=").LastOrDefault();

                if (args.Where(_ => _.StartsWith("--selectorType=")).Any())
                    inputParam.selectorType = (Input.SelectorType)Convert.ToInt16(args.Where(_ => _.StartsWith("--selectorType=")).FirstOrDefault().Split("=").LastOrDefault());


                if (args.Where(_ => _.StartsWith("--waittimeout=")).Any())
                {
                    inputParam.waitTimeout = Convert.ToInt32(args.Where(_ => _.StartsWith("--waittimeout=")).FirstOrDefault().Split("=").LastOrDefault());
                }

                if (args.Where(_ => _.StartsWith("--waitforexit=")).Any())
                {
                    inputParam.waitForExit = Convert.ToInt32(args.Where(_ => _.StartsWith("--waitforexit=")).FirstOrDefault().Split("=").LastOrDefault());
                }

                if (args.Where(_ => _.StartsWith("--cromedrivertimeout=")).Any())
                {
                    inputParam.cromeDriverTimeout = Convert.ToInt32(args.Where(_ => _.StartsWith("--cromedrivertimeout=")).FirstOrDefault().Split("=").LastOrDefault());
                }


                if (args.Where(_ => _.StartsWith("--iteration")).Any())
                {
                    inputParam.iteration = true;
                }

                if (args.Where(_ => _.StartsWith("--basedomain=")).Any())
                {
                    inputParam.baseDomain = args.Where(_ => _.StartsWith("--basedomain=")).FirstOrDefault().Split("=").LastOrDefault();
                }

                if (args.Where(_ => _.StartsWith("--basedomain=")).Any())
                {
                    inputParam.baseDomain = args.Where(_ => _.StartsWith("--basedomain=")).FirstOrDefault().Split("=").LastOrDefault();
                }

                if (args.Where(_ => _.StartsWith("--savepartial")).Any())
                {
                    inputParam.savePartial = true;
                }

                if (args.Where(_ => _.StartsWith("--file=")).Any())
                {
                    var file = args.Where(_ => _.StartsWith("--file=")).FirstOrDefault().Split("=").LastOrDefault();
                    var alllinks = ReadAllLines(file);

                    var count = 0;
                    foreach (var item in alllinks)
                    {

                        urls.Add(new Input
                        {
                            link = item,
                            iteration = count == 0 ? true : false,
                            whiteDomainList = inputParam.whiteDomainList,
                            blackList = inputParam.blackList,
                            selectorType = inputParam.selectorType,
                            selector = inputParam.selector,
                            waitForExit = inputParam.waitForExit,
                            waitTimeout = inputParam.waitTimeout,
                            cromeDriverTimeout = inputParam.cromeDriverTimeout,
                            savePartial = inputParam.savePartial,
                            baseDomain = inputParam.baseDomain,
                        }); ;
                        count++;
                    }
                }
                else
                {
                    urls.Add(inputParam);
                }

            }

            return urls;
        }

        private static string[] ReadAllLines(string? file)
        {
            // Especifica a codificação como UTF-8
            Encoding utf8Encoding = Encoding.UTF8;

            // Lê todas as linhas do arquivo com a codificação UTF-8
            string[] linhas;
            using (StreamReader reader = new StreamReader(file, utf8Encoding))
            {
                linhas = reader.ReadToEnd().Split(Environment.NewLine);
            }

            return linhas;
        }

        private void SaveValidLinks()
        {

            var clear_validLinks = _validLinks.Where(_ => !_invalidLinks.Where(__ => __.link == _.link).Any()).ToList();
            var filePath = "all-links.json";
            SaveLinks(filePath, clear_validLinks);
        }

        private void SaveInvalidLinks()
        {
            var filePath = "all-links.err";
            SaveLinks(filePath, _invalidLinks.ToList());
        }

        private async Task SendFilesOut()
        {
            var clientId = "...";
            var clientSecret = "...";

            var resultToken = await GetTokenSendFileOut(clientId, clientSecret);

            string[] files = Directory.GetFiles(_config.OutputFolderPathPDF, "*.pdf");

            if (!files.Any())
            {
                LogRed("Nehum arquivo de pdf encontrado em " + AppContext.BaseDirectory);
            }

            foreach (var file in files)
            {
                var errorCount = 0;
                await SendFileOutBase64(clientId, clientSecret, file, resultToken, errorCount);
            }

        }

        private async Task SendFileOutBinary(string clientId, string filename, JsonElement resultToken)
        {

            //var teste = "https://app-atento-knowledgeassistant.azurewebsites.net/admin/api/Clients/97b01507-e1ff-4a54-a6b2-bf925a55a139/FormFiles?api-version=1.0"
            var apiUrl = $"https://app-atento-knowledgeassistant.azurewebsites.net/admin/api/Clients/{clientId}/FormFiles?api-version=1.0";
            //var base64File = ConvertPdfToBase64(filename);

            resultToken.TryGetProperty("access_token", out var accessTokenElement);
            // Adicionando o cabeçalho de autenticação
            _clientHttp.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessTokenElement}");

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            var content = new MultipartFormDataContent();
            //content.Add(new StringContent(base64File), "Files");
            var filebin = new StreamContent(File.OpenRead(filename));
            content.Add(filebin);

            // Adicionando Content-Disposition ao cabeçalho de conteúdo, não aos cabeçalhos padrão
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"Files\""
            };

            request.Content = content;

            var response = await _clientHttp.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private async Task SendFileOutBase64(string clientId, string clientSecret, string filename, JsonElement resultToken, int errorCount)
        {

            try
            {
                var apiUrl = $"https://app-atento-knowledgeassistant.azurewebsites.net/admin/api/Clients/{clientId}/Files?api-version=1.0";
                var base64File = ConvertPdfToBase64(filename);
                resultToken.TryGetProperty("access_token", out var accessTokenElement);

                _clientHttp.DefaultRequestHeaders.Clear();
                _clientHttp.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessTokenElement}");

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

                var json = JsonSerializer.Serialize(new
                {
                    Files = new List<dynamic> {
                    new {
                        name = Path.GetFileName(filename),
                        base64= base64File
                    }
                }
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await _clientHttp.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseMsg = await response.Content.ReadAsStringAsync();
                LogGreen(responseMsg);
            }
            catch (Exception ex)
            {
                LogRed($"{ex.Message} tentativa {errorCount}");

                if (errorCount < 5)
                {
                    errorCount++;
                    Thread.Sleep(1000 * errorCount);
                    var resultToken2 = await GetTokenSendFileOut(clientId, clientSecret);
                    await SendFileOutBase64(clientId, clientSecret, filename, resultToken2, errorCount);
                }
            }
        }
        private async Task<JsonElement> GetTokenSendFileOut(string clientId, string clientSecret)
        {
            //Obter Token Azure AD Client Credencial
            var paramsUrlToken = new Dictionary<string, string>() {

                {"grant_type" , "client_credentials" },
            };

            var urlToken = "https://mssecurity.atento.com.br/v2.0/connect/token";
            var requestmsgToken = new HttpRequestMessage(HttpMethod.Post, urlToken)
            {
                Content = new FormUrlEncodedContent(paramsUrlToken)
            };

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
            requestmsgToken.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            var resToken = await _clientHttpToken.SendAsync(requestmsgToken);
            var dataToken = await resToken.Content.ReadAsStringAsync();
            var resultToken = JsonSerializer.Deserialize<JsonElement>(dataToken);
            return resultToken;
        }

        static string ConvertPdfToBase64(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(fileBytes);
        }
        private async Task ConvertMainUrls(List<Input> inputs)
        {
            foreach (var input in inputs)
            {
                await ConvertContentHtmlPDF(input);
            }

        }

        string ReplaceRemoteImagePaths(string html)
        {
            // Usar expressão regular para encontrar todas as chamadas remotas de imagens
            string pattern = @"<img[^>]*\bsrc\s*=\s*['""]([^'""]*)['""]";
            var regex = new Regex(pattern);

            // Função de substituição personalizada
            string ReplaceMatch(Match match)
            {
                string remotePath = match.Groups[1].Value;
                string fileName = Path.GetFileName(remotePath);
                string localPath = Path.Combine(_config.OutputFolderPathHtmlImages, fileName);

                // Substituir a chamada remota pela chamada local
                return $"<img src='{localPath}'";
            }

            // Aplicar a substituição na string HTML
            string modifiedHtml = regex.Replace(html, ReplaceMatch);

            return modifiedHtml;
        }

        private async Task ExtractLinks(List<Input> inputs)
        {

            foreach (var input in inputs.Where(_ => _.iteration))
            {
                await GetLink(input);
            }

            Parallel.ForEach(inputs.Where(_ => !_.iteration), async (input) =>
            {
                await GetLink(input);
            });


            SaveValidLinks();

            SaveInvalidLinks();

            //SaveLinksDataverse();
        }

        private async Task ProcessFilesLinks()
        {
            string[] txtFiles = Directory.GetFiles(_config.OutputFolderPathLinks, "*.json");

            if (!txtFiles.Any())
            {
                LogRed("Nehum arquivo de link encontrado em " + AppContext.BaseDirectory);
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
                var contentFile = ReadAllText(inputFilePath);

                var inputs = JsonSerializer.Deserialize<List<Input>>(contentFile);
                LogGreen("Processando " + inputs.Count() + " links do aquivo " + inputFilePath);

                foreach (Input input in inputs)
                {
                    await ConvertContentHtmlPDF(input);
                };


            }
            catch (Exception ex)
            {
                LogRed("Ocorreu um erro: " + ex.Message);
            }
        }

        private static string ReadAllText(string inputFilePath)
        {
            // Especifica a codificação como UTF-8
            Encoding utf8Encoding = Encoding.UTF8;
            // Lê o conteúdo do arquivo com a codificação UTF-8
            string conteudoLido;
            using (StreamReader reader = new StreamReader(inputFilePath, utf8Encoding))
            {
                conteudoLido = reader.ReadToEnd();
            }

            return conteudoLido;
        }
        private async Task ConvertContentHtmlPDF(Input input)
        {
            var errorCount = 0;
            var content = await GetHtmlFromUrlBySelenium(input, errorCount);
            await ConvertContentHtmlPDF(input, content, errorCount);
        }
        private async Task ConvertContentHtmlPDF(Input input, string content, int erroCount)
        {

            try
            {

                var result = content;
                result = ReplaceHtmlContent(content);
                //result = ReplaceRemoteImagePaths(content);

                var fileNameHtml = Path.Combine(_config.OutputFolderPathHtml, input.filenameHTML);
                SaveHtml(input, result, fileNameHtml);

                //await imagesDownload(input, _driver);

                var fileNamePDF = Path.Combine(_config.OutputFolderPathPDF, input.filenamePDF);

                if (_config.PdfMethod == WorkerConfig.PdfMethodType.WkHtmltoPdf)
                    SavePdfFromHtmlWkhtmltopdf(input, fileNameHtml, Path.GetFullPath(fileNamePDF));

                if (_config.PdfMethod == WorkerConfig.PdfMethodType.Chrome)
                    SavePdfFromHtmlChrome(input, Path.GetFullPath(fileNameHtml), Path.GetFullPath(fileNamePDF));

                if (_config.PdfMethod == WorkerConfig.PdfMethodType.ChromeDrive)
                    SavePdfFromHtmlChromeDriver(input, Path.GetFullPath(fileNameHtml), Path.GetFullPath(fileNamePDF));

            }
            catch (Exception ex)
            {
                var error = $"# Ocorreu um erro no link: {input.link} [" + ex.Message + "]  adicionado como invalido";
                LogRed(error);

                _invalidLinks.Add(new Input
                {

                    id = _invalidLinks.Count(),
                    link = input.link,
                    selector = input.selector,
                    iteration = input.iteration,
                    selectorType = input.selectorType,
                    error = error,
                    sourceId = input.id,
                    savePartial = input.savePartial,
                    baseDomain = input.baseDomain,

                });
            }
        }


        private void SaveHtml(Input input, string content, string fileNameHtml)
        {
            // Salve o HTML no arquivo
            //File.WriteAllTextAsync(fileNameHtml, content);
            WriteAllText(content, fileNameHtml);
            LogGreen("HTML " + input.id + " da página " + input.link + " salvo em " + fileNameHtml);
        }

        private async Task GetLink(Input input)
        {
            string filePath = GetValidFileName(input.link + ".log");

            try
            {

                var errorCount = 0;
                var content = await GetHtmlFromUrlBySelenium(input, errorCount);
                if (string.IsNullOrEmpty(content))
                {
                    _invalidLinks.Add(input);
                    LogYellow("* Link " + input.link + " adicionado como invalido [html vazio]");
                    return;
                }

                LogYellow("* Link " + input.link + " adicionado como valido");
                _validLinks.Add(input);

                await ConvertContentHtmlPDF(input, content, errorCount);

                LogGreen($"[ Exitem {_validLinks.Count()} links validos, e {_invalidLinks.Count} invalidos ]");

                var sublinks = GetLinksFromHtml(content, input, filePath);

                if (sublinks.Count > 0)
                {
                    var newlinks = sublinks.Where(link => !_validLinks.Any(validLink => validLink.link == link.link)).ToList();
                    SaveLinks(filePath, newlinks);


                    foreach (var link in newlinks)
                    {
                        await GetLink(link);
                    }
                }
                else
                {
                    LogYellow("Nenhum link encontrado na página " + input.link);
                }
            }
            catch (Exception ex)
            {
                LogRed("Ocorreu um erro: para acessar " + input.link + ": " + ex.Message);
            }
        }

        private void SaveLinks(string filePath, List<Input> links)
        {
            var json = JsonSerializer.Serialize(links);
            var path = Path.Combine(_config.OutputFolderPathLinks, filePath.Replace(".log", $"-{links.Count()}.log"));
            //File.WriteAllText(path, json);

            WriteAllText(json, path);

            LogGreen(links.Count + " links extraídos e salvos em " + filePath);
        }

        private static void WriteAllText(string json, string path)
        {
            // Especifica a codificação como UTF-8
            Encoding utf8Encoding = Encoding.UTF8;
            // Escreve o conteúdo no arquivo com a codificação UTF-8
            using (StreamWriter writer = new StreamWriter(path, false, utf8Encoding))
            {
                writer.Write(json);
            }
        }


        HashSet<Input> GetLinksFromHtml(string html, Input input, string filePath)
        {
            var links = new HashSet<Input>();
            var baseDomain = input.GetBaseDomain();


            // Expressão regular para encontrar links
            string pattern = @"<a\s+(?:[^>]*?\s+)?href\s*=\s*[""']([^""']*)[""']";
            var id = 0;
            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase))
            {
                try
                {
                    var linkextract = match.Groups[1].Value;

                    if (!linkextract.StartsWith("http") || !linkextract.StartsWith("https"))
                    {
                        //var linkextractComplete = baseDomain + "/" + linkextract;
                        var linkextractComplete = new Uri(new Uri(baseDomain), linkextract);
                        linkextractComplete = applyRulesByBaseDomain(baseDomain, linkextract, linkextractComplete);

                        LogYellow($"> Corrigingo link {linkextract} para {linkextractComplete}");
                        linkextract = linkextractComplete.ToString();
                    }

                    if (linkextract.Contains("javascript:void(0)"))
                    {
                        LogYellow($"# link {linkextract} ignorado - [javascript:void(0)]");
                        continue;
                    }

                    if (input.blackList.Any(item => linkextract.Contains(item)))
                    {
                        LogYellow($"# link {linkextract} ignorado - [bloqueado]");
                        continue;
                    }

                    if (input.whiteDomainList.Any())
                    {
                        if (!input.whiteDomainList.Any(item => linkextract.Contains(item)))
                        {
                            LogYellow($"# link {linkextract} ignorado - [não liberado]");
                            continue;

                        }
                    }

                    if (linkextract.Replace("/", "") == baseDomain.Replace("/", ""))
                    {
                        LogYellow($"# link {linkextract} ignorado - [igua a raiz]");
                        continue;

                    }

                    if (linkextract == input.link)
                    {

                        LogYellow($"# link {linkextract} ignorado - [igua a raiz]");
                        continue;

                    }

                    if (linkextract.Split('#').FirstOrDefault() == input.link.Split('#').FirstOrDefault())
                    {
                        LogYellow($"# link {linkextract} ignorado - [ancora para raiz]");
                        continue;
                    }


                    if (_validLinks.Where(_ => _.link == linkextract).Any())
                    {
                        LogRed($"# link {linkextract} ignorado - [já extraido anteriomente]");
                        continue;
                    }


                    if (!links.Where(_ => _.link == linkextract).Any())
                    {
                        links.Add(new Input
                        {
                            id = id++,
                            link = linkextract,
                            selector = input.selector,
                            iteration = false,
                            selectorType = input.selectorType,
                            prefixo = "sub-" + filePath,
                            sourceId = input.id,
                            blackList = input.blackList,
                            whiteDomainList = input.whiteDomainList,
                            waitForExit = input.waitForExit,
                            waitTimeout = input.waitTimeout,
                            cromeDriverTimeout = input.cromeDriverTimeout,
                            savePartial = input.savePartial,
                            baseDomain = input.baseDomain,
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogRed($"# link {input.link} ignorado - [{ex.Message}]");
                }
            }

            return links;
        }

        private static Uri applyRulesByBaseDomain(string baseDomain, string linkextract, Uri linkextractComplete)
        {
            if (baseDomain == "https://atento-vivo.inpaas.com/api/ckb-portal-online/portal-vivo/pt/")
            {
                LogYellow($"> aplicando regras de {baseDomain}");

                var linkextractClear = linkextract
                    .Replace("../pt/", "")
                    .Replace("../", "")
                    .Replace("./", "");


                linkextractComplete = new Uri(new Uri(baseDomain), linkextractClear);
            }

            return linkextractComplete;
        }


        async Task<string> GetHtmlFromUrlBySelenium(Input input, int errorCount)
        {
            var pageSource = "";

            ChromeDriver driver = GetInstaceSeleniumWebDriver(input);

            try
            {
                LogGreen($"navegando para: {input.link}");

                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(input.cromeDriverTimeout); // Ajuste o tempo limite conforme necessário

                driver.Navigate().GoToUrl(HttpUtility.UrlDecode(input.link));

                // Aguarda até que a página esteja totalmente carregada
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));


                if (input.iteration)
                {
                    while (true)
                    {
                        LogYellow("Quando desejar seguir digite S");

                        var continueprocess = Console.ReadLine();
                        if (continueprocess.ToUpper() == "S")
                        {
                            input.iteration = false;
                            break;
                        }
                    }

                }
                else if (input.waitTimeout > 0)
                {
                    LogYellow($"Aguardando {input.waitTimeout} milesegundos...");
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

                    if (input.selectorType == Input.SelectorType.classname)
                    {
                        IWebElement elemento = driver.FindElement(By.ClassName(input.selector));
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
                //driver.Dispose();
            }
            catch (Exception ex)
            {
                var error = $"Erro ao navegar para: {input.link} [{ex.Message}] tentativa:{errorCount}";
                LogRed(error);

                errorCount++;

                if (driver != null)
                {
                    //driver.Quit();
                    //driver.Dispose();
                }

                if (errorCount < _config.TryCount)
                {
                    input.cromeDriverTimeout = input.cromeDriverTimeout * errorCount;
                    LogYellow("CromeDriverTimeout set to " + input.cromeDriverTimeout);
                    return await GetHtmlFromUrlBySelenium(input, errorCount);
                }
                input.cromeDriverTimeout = 120;
                //throw ex;
            }

            return pageSource;

        }



        private string ReplaceHtmlContent(string pageSource)
        {
            var result = pageSource;
            foreach (var element in _config.ReplaceHtml)
            {

                result = result.Replace(element.Key, element.Value);
            }

            return result;
        }

        // Método para fazer o download da imagem
        void DownloadImage(IWebDriver driver, string imageUrl)
        {
            var fileName = Path.GetFileName(imageUrl);

            var outputPath = Path.Combine(_config.OutputFolderPathHtml, _config.OutputFolderPathHtmlImages, fileName);

            // Executa um script JavaScript para obter o conteúdo da imagem como base64
            var imageContentBase64 = ((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].toDataURL('image/png').substring(21);", driver.FindElement(By.CssSelector($"img[src='{imageUrl}']")));

            // Converte o conteúdo base64 de volta para bytes
            var imageBytes = Convert.FromBase64String(imageContentBase64.ToString());

            // Salva os bytes da imagem em um arquivo
            File.WriteAllBytes(outputPath, imageBytes);

            Console.WriteLine($"Imagem salva como: {fileName}");
        }

        private async Task imagesDownload(Input input, IWebDriver driver)
        {
            LogGreen($"Baixando as imagens da pagina {input.link}");


            var images = driver.FindElements(By.TagName("img"));
            foreach (var image in images)
            {
                try
                {
                    var imageUrl = image.GetAttribute("src");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var fileName = Path.GetFileName(imageUrl);
                        var outputPath = Path.Combine(_config.OutputFolderPathHtml, _config.OutputFolderPathHtmlImages, fileName);

                        var response = await _clientHttp.GetAsync(imageUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var imageBytes = await response.Content.ReadAsByteArrayAsync();
                            await File.WriteAllBytesAsync(outputPath, imageBytes);
                            LogGreen($"Imagem salva: {outputPath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogRed($"Ocorreu um erro em imagesDownload: {ex.Message}");
                }
            }

        }

        private static void LogGreen(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] - {msg}");
            Console.ResetColor();
        }

        private static void LogYellow(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now}] - {msg}");
            Console.ResetColor();
        }

        private static void LogRed(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] - {msg}");
            Console.ResetColor();
        }

        private ChromeDriver GetInstaceSeleniumWebDriver(Input input)
        {
            if (_driver != null)
                return _driver;

            string driverPath = _config.DriverPath;

            // Configurar as opções do Chrome
            var chromeOptions = new ChromeOptions();
            //chromeOptions.Proxy = Proxy();

            chromeOptions.PageLoadStrategy = PageLoadStrategy.Normal; // ou PageLoadStrategy.Eager

            if (input.iteration)
                _config.AddArgument = _config.AddArgument.Replace("--headless", "");

            var arguments1 = _config.AddArgument.Split("--");
            foreach (var item in arguments1.Where(_ => !string.IsNullOrEmpty(_)))
            {
                chromeOptions.AddArgument($"--{item}");
            }

            //chromeOptions.AddUserProfilePreference("profile.managed_default_content_settings.javascript", 1);

            var driver = new ChromeDriver(driverPath, chromeOptions);
            _driver = driver;
            return driver;
        }

        private Proxy Proxy()
        {
            var proxy = new Proxy();
            proxy.Kind = ProxyKind.Manual;
            proxy.IsAutoDetect = false;
            proxy.HttpProxy = _config.Proxy;
            proxy.SslProxy = _config.Proxy;
            return proxy;
        }

        void SavePdfFromHtmlChromeDriver(Input input, string htmlFilePath, string pdfFilePath)
        {
            // Output a PDF of the first page in A4 size at 90% scale
            var printOptions = new Dictionary<string, object>
            {
                { "paperWidth", 210 / 25.4 },
                { "paperHeight", 297 / 25.4 },
                { "scale", 0.9 },
            };

            var printOutput = _driver.ExecuteChromeCommandWithResult("Page.printToPDF", printOptions) as Dictionary<string, object>;
            var pdf = Convert.FromBase64String(printOutput["data"] as string);
            File.WriteAllBytesAsync(pdfFilePath, pdf);

            if (File.Exists(pdfFilePath))
            {
                LogGreen("ChromeDriver - PDF " + input.id + " da página " + input.link + " salvo em " + pdfFilePath);
            }
            else
            {
                var error = "ChromeDriver - PDF da página " + input.link + " não encontrado em " + pdfFilePath;
                LogRed(error);
                throw new Exception(error);
            }

        }
        void SavePdfFromHtmlChrome(Input input, string htmlFilePath, string pdfFilePath)
        {
            var p = new System.Diagnostics.Process()
            {
                StartInfo =
                {
                    FileName = _config.ChromePath,
                    //FileName = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
                    //Arguments = $@"/C --headless --disable-gpu --run-all-compositor-stages-before-draw --no-pdf-header-footer --print-to-pdf=""{pdfFilePath}"" ""{htmlFilePath}""",
                    //Arguments = $@"/C {_config.AddArgument2}--print-to-pdf=""{pdfFilePath}"" ""{input.link}""",
                    Arguments = $@"/C {_config.AddArgument2} --print-to-pdf=""{pdfFilePath}"" ""{htmlFilePath}""",
                }
            };

            p.Start();

            // ...then wait n milliseconds for exit (as after exit, it can't read the output)
            p.WaitForExit(input.waitForExit);

            // read the exit code, close process
            int returnCode = p.ExitCode;
            p.Close();

            if (File.Exists(pdfFilePath))
            {
                LogGreen("Chrome - PDF " + input.id + " da página " + input.link + " salvo em " + pdfFilePath);
            }
            else
            {
                var error = "Chrome - PDF da página " + input.link + " não encontrado em " + pdfFilePath;
                LogRed(error);

                throw new Exception(error);
            }

        }


        void SavePdfFromHtmlWkhtmltopdf(Input input, string htmlFilePath, string pdfFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _config.Pdfexe;
            startInfo.Arguments = $"{_config.PdfArguments}{htmlFilePath} {pdfFilePath}";
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
                LogGreen("Wkhtmltopdf - PDF " + input.id + " da página " + input.link + " salvo em " + pdfFilePath);

            }
            else
            {
                var error = "Wkhtmltopdf - PDF da página " + input.link + " não encontrado em " + pdfFilePath;
                LogGreen(error);
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
