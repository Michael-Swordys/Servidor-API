using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }
    private int Porta { get; set; }
    private int QtdeRequests { get; set; }

    private string HtmlExemplo { get; set; }
    public SortedList<string, string> TiposMime { get; set; }



    //Define a porta e IP do Servidor e trata se o mesmo está rodando
    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;
        this.CriarHtmlExemplo();
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
                var bytesConteudo = LerArquivo(recursoBuscado);
                if (bytesConteudo.Length > 0)
                {
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", 200, bytesConteudo.Length);
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





    //Esta função ler o arquivo e verifica se o mesmo existe





    public Byte[] LerArquivo(string recurso)
    {
        string diretorio = "C:\\Users\\Dev003\\Documents\\Servidor-HTTP\\ServidorHttp\\wwwDinamico";
        string caminho = diretorio + recurso.Replace("/", "\\");
        if (File.Exists(caminho))
        {
            return File.ReadAllBytes(caminho);
        }
        return new byte[0];
    }


    //Adiciona ao servidor a capacidade ler e interpretar tipos de arquivos comuns na Web 



    private void InserirTypesMime()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html", "text/html;charset=utf-8"); //HTML
        this.TiposMime.Add(".htm", "text/html;charset=utf-8"); //HTML
        this.TiposMime.Add(".css", "text/css"); //Css
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
    }
}