using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }
    private int Porta { get; set; }
    private int QtdeRequests { get; set; }

    private string HtmlExemplo { get; set; }
    private SortedList<string, string> TiposMime { get; set; }
    private SortedList<string, string> DiretoriosHost { get; set; }



    //Define a porta e IP do Servidor e trata se o mesmo está rodando


    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;
        this.CriarHtmlExemplo();
        this.InserirTypesMime();
        this.PopularDiretorioHost();
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor está rodando com acesso a porta {this.Porta}.");
            Console.WriteLine($"Acesse: http://localhost:{this.Porta}");
            Task ServidorHttpTask = Task.Run(() => AguardarRequests());
            ServidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao inicia servidor na porta {this.Porta}:\n{e.Message}");
        }
    }


    //Aguarda uma resquest e fica em Loop aguardando outra com o Await


    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }


    //Função que solicita e verifica nossa propriedade "Conexão" está conectada.
    //Faz o tratamento dos Bytes ( Já que um servidor aceita somente Bytes ) então precisando converter nossos tipos para bytes em nossa request e response.
    //Também realiza o tratamento de recurso solicitado na request.


    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected)
        {
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            string textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao)
                .Replace((char)0, ' ').Trim();
            if (textoRequisicao.Length > 0)
            {
                Console.WriteLine($"\n{textoRequisicao}\n");
                string[] linhas = textoRequisicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(
                iPrimeiroEspaco + 1, iSegundoEspaco - iPrimeiroEspaco - 1);
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost = linhas[1].Substring(iPrimeiroEspaco + 1);
                byte[] bytesCabecalho = null;
                byte[] bytesConteudo = null;
                FileInfo FileInfomation = new FileInfo(DiretorioFisico(nomeHost, recursoBuscado));
                if (FileInfomation.Exists)
                {
                    if (TiposMime.ContainsKey(FileInfomation.Extension.ToLower()))
                    {
                        bytesConteudo = File.ReadAllBytes(FileInfomation.FullName);
                        string tipoMime = TiposMime[FileInfomation.Extension.ToLower()];

                        bytesCabecalho = GerarCabecalho(versaoHttp, tipoMime, 200, bytesConteudo.Length);
                    }
                }
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes(@"<h1>Erro 404 - Arquivo não encontrado <\h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", 404, bytesConteudo.Length);
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);

                conexao.Close();
                Console.WriteLine($"\n {bytesEnviados} bytes foram enviados a requisição {numeroRequest}");

            }
        }
        Console.WriteLine($"Resquest {numeroRequest} Finalizada...");

    }


    //Adicionado, populando string's na URL e definindo alguns tipos --> ex: VersãoHttp.


    public byte[] GerarCabecalho(string versaoHttp, string tipoMime,
       int codigoHttp, int qtdeBytes = 0)
    {
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0{Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }


    //Função criada no começo do desenvolvimento desta aplicação mas descidi não utiliza-lá no momento, ela será utilizada futuramente e seu codigo será reutilizado e refatorado.


    private void CriarHtmlExemplo()
    {
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title>http</title></head><body>");
        html.Append("<h1>Página Estática</h1></body></html>");
        this.HtmlExemplo = html.ToString();
    }


    //Adiciona ao servidor a capacidade ler e interpretar tipos de arquivos comuns na Web 

    private void InserirTypesMime()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html", "text/html;charset=utf-8"); //HTML
        this.TiposMime.Add(".htm", "text/html;charset=utf-8"); //HTML
        this.TiposMime.Add(".css", "text/css"); //Css
        this.TiposMime.Add(".js", "text/javascript");// JavaScript programming 
        this.TiposMime.Add(".aac", "audio/aac"); //Audio
        this.TiposMime.Add(".abw", "application/x-abiword"); //Document
        this.TiposMime.Add(".arc", "application/x-freearc"); //Archive document (multiple files embedded)
        this.TiposMime.Add(".avi", "video/x-msvideo");//AVI: Audio Video Interleave
        this.TiposMime.Add(".azw", "application/vnd.amazon.ebook");//application/vnd.amazon.ebook
        this.TiposMime.Add(".bin", "application/octet-stream");//Any kind of binary data
        this.TiposMime.Add(".bmp", "image/bmp");//Windows OS/2 Bitmap Graphics
        this.TiposMime.Add(".bz", "application/x-bzip");//BZip archive
        this.TiposMime.Add(".bz2", "application/x-bzip2");//BZip2 archive
        this.TiposMime.Add(".csh", "application/x-csh");//Shell script
        this.TiposMime.Add(".csv", "texte/csv");//Comma-separated values (CSV)	
        this.TiposMime.Add(".doc", "application/msword");//Microsoft Word	
        this.TiposMime.Add("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");//Microsoft Word (OpenXML)
        this.TiposMime.Add(".eot", "application/vnd.ms-fontobject");//MS Embedded OpenType fonts	
        this.TiposMime.Add(".epub", "application/epub+zip");//Electronic publication (EPUB)
        this.TiposMime.Add(".gz", "application/gzip");//GZip Compressed Archive	
        this.TiposMime.Add(".gif", "image/gif");//Graphics Interchange Format (GIF)	
        this.TiposMime.Add(".ico", "image/vnd.microsoft.icon");//Icon format
        this.TiposMime.Add(".ics", "text/calendar");//iCalendar format	
        this.TiposMime.Add(".jar", "application/java-archive");//Java Archive (JAR)	
        this.TiposMime.Add(".jpeg", "image/jpeg");//JPEG images	
        this.TiposMime.Add(".jpg", "image/jpeg");//JPEG images
        this.TiposMime.Add(".json", "application/json");// Application/json
        this.TiposMime.Add(".jsonld", "application/ld+json");// Json-LD
        this.TiposMime.Add(".midi", "audio/midi audio/x-midi");// Musical Instrument Digital Interface (MIDI)
        this.TiposMime.Add(".mid", "audio/midi audio/x-midi");// Musical Instrument Digital Interface (MIDI)	
        this.TiposMime.Add(".mjs", "text/javascript");//JavaScript module
        this.TiposMime.Add(".mp3", "audio/mpeg"); //Mp3 audio
        this.TiposMime.Add(".mpeg", "video/mpeg");//MPEG video
        this.TiposMime.Add(".mpkg", "application/vnd.apple.installer+xml");// Apple Installer package
        this.TiposMime.Add(".odp", "application/vnd.oasis.opendocument.presentation");//OpenDocument presentation document
        this.TiposMime.Add(".ods", "application/vnd.oasis.opendocument.spreadsheet");//OpenDocument spreadsheet document	
        this.TiposMime.Add(".odt", "application/vnd.oasis.opendocument.text");//OpenDocument text document	
        this.TiposMime.Add(".oga", "audio/ogg");//OGG audio
        this.TiposMime.Add(".ogv", "video/ogg");//OGG video
        this.TiposMime.Add(".ogx", "application/ogg");//OGG	
        this.TiposMime.Add(".opus", "audio/opus");//	Opus audio
        this.TiposMime.Add(".otf", "font/otf");//OpenType font	
        this.TiposMime.Add(".png", "image/png");//Portable Network Graphics
        this.TiposMime.Add(".pdf", "application/pdf");//Adobe Portable Document Format (PDF)
        this.TiposMime.Add(".php", "application/x-httpd-php");//Hypertext Preprocessor (Personal Home Page)	
        this.TiposMime.Add(".ppt", "application/vnd.ms-powerpoint");// Microsoft PowerPoint
        this.TiposMime.Add(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");//Microsoft PowerPoint (OpenXML)	
        this.TiposMime.Add(".rar", "application/vnd.rar");// RAR archive	
        this.TiposMime.Add(".rtf", "application/rtf");// Rich Text Format (RTF)
        this.TiposMime.Add(".sh", "application/x-sh");//Bourne shell script	
        this.TiposMime.Add(".svg", "image/svg+xml");//	Scalable Vector Graphics (SVG)
        this.TiposMime.Add(".swf", "application/x-shockwave-flash");//Small web format (SWF) or Adobe Flash document
        this.TiposMime.Add(".tar", "application/x-tar");// Tape Archive (TAR)	
        this.TiposMime.Add(".tif", "image/tiff");// Tagged Image File Format (TIFF)
        this.TiposMime.Add(".tiff", "image/tiff");// Tagged Image File Format (TIFF)
        this.TiposMime.Add(".ts", "video/mp2t");// MPEG transport stream	
        this.TiposMime.Add(".ttf", "font/ttf");//TrueType Font	
        this.TiposMime.Add(".txt", "text/plain");//Text (generally ASCII or ISO 8859-n)
        this.TiposMime.Add(".vsd", "application/vnd.visi");// Microsoft Visio
        this.TiposMime.Add(".wav", "audio/wav");// Waveform Audio Format
        this.TiposMime.Add(".weba", "audio/webm");// WEBM audio
        this.TiposMime.Add(".webm", "video/webm");//WEBM video
        this.TiposMime.Add(".webp", "image/webp");//WEBP image
        this.TiposMime.Add(".woff", "font/woff");//Web Open Font Format (WOFF)	
        this.TiposMime.Add(".woff2", "font/woff2");//Web Open Font Format (WOFF)
        this.TiposMime.Add(".xhtml", "application/xhtml+xml");//XHTML
        this.TiposMime.Add(".xls", "application/vnd.ms-excel");// Microsoft Excel
        this.TiposMime.Add(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");//Microsoft Excel (OpenXML)	
        this.TiposMime.Add(".xml", "application/xml");//if not readable from casual users (RFC 3023, section 3)text/xml if readable from casual users (RFC 3023, section 3)
        this.TiposMime.Add(".xul", "application/vnd.mozilla.xul+xml");//	XUL
        this.TiposMime.Add(".zip", "application/zip");//ZIP archive
        this.TiposMime.Add(".3gp", "video/3gpp");// audio/video container -> if it doesn't contain video = audio/3gpp
        this.TiposMime.Add(".3g2", "video/3gpp2");// 3GPP2 audio/video container if it doesn't contain video audio/3gpp
        this.TiposMime.Add(".7z", "application/x-7z-compressed");//7-zip archive	    
    }


    public void PopularDiretorioHost()
    {
        this.DiretoriosHost = new SortedList<string, string>();
        this.DiretoriosHost.Add("localhost", "C:\\Users\\Dev003\\Documents\\Servidor-HTTP\\ServidorHttp\\www\\localhost");
        this.DiretoriosHost.Add("michael.com", "C:\\Users\\Dev003\\Documents\\Servidor-HTTP\\ServidorHttp\\www\\michael.com");

    }


    //Verifica se o arquivo existe e retorna o mesmo.

    public string DiretorioFisico(string host, string arquivo)
    {
        string diretor = this.DiretoriosHost[host.Split(":")[0]];
        string caminhoArquivo = diretor + arquivo.Replace("/", "\\");
        return caminhoArquivo;
    }

}