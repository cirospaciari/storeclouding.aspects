using StoreClouding.Aspects.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StoreClouding.Aspects.Net
{
    /// <summary>
    ///  Substitui corpo do método por chamada HTTP
    /// Corpo do método é chamada em caso de Exception, que pode ser obtida em HttpRequest.LastError
    /// </summary>
    public class HttpRequest : PostSharp.Aspects.MethodInterceptionAspect
    {
        private string Uri { get; set; }
        /// <summary>
        /// Tipo de Método utilizado na requisição HTTP
        /// </summary>
        private HttpRequestMethod Method { get; set; }
        /// <summary>
        /// Tipo de resposta recebida pela requisição HTTP
        /// </summary>
        private HttpRequestResponseType ResponseType { get; set; }
        /// <summary>
        /// Tipo de conteudo enviado pela requisição HTTP
        /// </summary>
        private HttpRequestContentType ContentType { get; set; }

        /// <summary>
        /// Tag de sobreescrita de configuração AppSetting
        /// </summary>
        private const string AppSettingTag = "@appSetting:";
        /// <summary>
        /// Headers para chamada HTTP pode ser usado {nomeDoArgumento} para preenchimento dinamico,
        /// ele pode ser separado por quebra de linha(\n) para passagem de mais de um Header
        /// </summary>
        public string Header { get; set; }
        /// <summary>
        /// Adiciona nome do método a URL
        /// </summary>
        public bool MethodNameInUrl { get; set; }
        /// <summary>
        /// Envia métodos PUT, DELETE e PATCH como se fossem POST e com os headers
        ///  X-HTTP-Method-Override, X-HTTP-Method e X-METHOD-OVERRIDE marcados com o nome do método
        /// </summary>
        public bool XHttpRequestOverride { get; set; }
        /// <summary>
        /// Habilita ou desabilita Expect100Continue
        /// </summary>
        public bool Expect100Continue { get; set; }
        /// <summary>
        /// Tempo de Timeout em millisegundos (120 segundos por padrão)
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// true se a solicitação para o recurso de Internet deve conter um cabeçalho HTTP Connection com o valor keep-alive; caso contrário, false. O padrão é true.
        /// </summary>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Ultimo Exception disparado no Aspecto
        /// </summary>
        private static Exception lastError = null;
        /// <summary>
        /// Ultimo Exception disparado no Aspecto
        /// </summary>
        public static Exception LastError
        {
            get
            {
                return lastError;
            }
        }
        /// <summary>
        /// Logger para disparos de Exceptions ocorridos
        /// </summary>
        public static IMethodInterceptionAspectExceptionLogger Logger { get; set; }
        /// <summary>
        /// Substitui corpo do método por chamada HTTP
        /// Corpo do método é chamada em caso de Exception, que pode ser obtida em HttpRequest.LastError
        /// </summary>
        /// <param name="url">Url em que será feita a chamada HTTP pode ser usado os coringas {nomeDoParametro} para substituição por parametro, @appSetting:NomeDaConfiguracao para substituição por configuração do AppSetting, @className e @methodName para substituição pelo nome da Classe e do Método</param>
        /// <param name="method">Método utilizado</param>
        /// <param name="responseType">Tipo de resposta</param>
        /// <param name="contentType">Tipo de conteudo enviado</param>
        public HttpRequest(string url,
                           HttpRequestMethod method = HttpRequestMethod.GET,
                           HttpRequestResponseType responseType = HttpRequestResponseType.JSON,
                           HttpRequestContentType contentType = HttpRequestContentType.FormData)
        {
            Uri = url;
            Method = method;
            ResponseType = responseType;
            ContentType = contentType;
            Timeout = 120000;
            KeepAlive = true;
        }

        public override void OnInvoke(PostSharp.Aspects.MethodInterceptionArgs args)
        {
            try
            {
                System.Net.ServicePointManager.Expect100Continue = Expect100Continue;

                string url = Uri.ToString();
                if (url == null)
                    url = args.Method.Name;
                else
                {
                    url = url.Replace("@className", args.Method.ReflectedType.Name);
                    url = url.Replace("@methodName", args.Method.Name);
                }
                //replace nas tags de app seting
                while (url.Contains(AppSettingTag))
                {
                    var index = url.IndexOf(AppSettingTag);
                    var barra = url.IndexOf('/');
                    if (barra == -1)
                        barra = url.IndexOf('\\');
                    if (barra == -1)
                        barra = url.Length;

                    var appSetting = url.Substring(index, barra);

                    var value = ConfigurationManager.AppSettings[appSetting.Split(':').Last()];

                    url = url.Replace(appSetting, value ?? string.Empty);
                }

                if (MethodNameInUrl)
                    url += (url.EndsWith("/") || url.EndsWith("\\")) ? args.Method.Name : "/" + args.Method.Name;


                var values = new Dictionary<string, object>();
                var parameters = args.Method.GetParameters();

                //cria parametros na url e valores a serem enviados por get ou post
                for (var i = 0; i < parameters.Length; i++)
                {

                    if (url.Contains("{" + parameters[i].Name + "}"))
                        url = url.Replace("{" + parameters[i].Name + "}", System.Web.HttpUtility.UrlEncode((args.Arguments[i] ?? string.Empty).ToString()));
                    else if (Header != null && Header.Contains("{" + parameters[i].Name + "}"))
                        Header = Header.Replace("{" + parameters[i].Name + "}", (args.Arguments[i] ?? string.Empty).ToString());
                    else
                        values.Add(parameters[i].Name, args.Arguments[i]);
                }

                byte[] result;
                switch (Method)
                {
                    case HttpRequestMethod.PUT:
                    case HttpRequestMethod.DELETE:
                    case HttpRequestMethod.PATCH:
                    case HttpRequestMethod.POST:
                        result = Post(url, values);
                        break;
                    case HttpRequestMethod.GET:
                    default:
                        result = Get(url, values);
                        break;
                }
                if ((args.Method as MethodInfo).ReturnType == typeof(void))
                    return;

                string text;
                switch (ResponseType)
                {
                    case HttpRequestResponseType.JSON:
                        text = Encoding.UTF8.GetString(result);
                        args.ReturnValue = Newtonsoft.Json.JsonConvert.DeserializeObject(text,
                                                                                        (args.Method as MethodInfo).ReturnType);
                        break;
                    case HttpRequestResponseType.XML:
                        text = Encoding.UTF8.GetString(result);
                        using (var reader = new StringReader(text))
                        {
                            XmlSerializer xsResponse = new XmlSerializer((args.Method as MethodInfo).ReturnType);
                            args.ReturnValue = xsResponse.Deserialize(reader);
                        }
                        break;
                    case HttpRequestResponseType.String:
                        args.ReturnValue = Encoding.UTF8.GetString(result);
                        break;
                    case HttpRequestResponseType.Bytes:
                    default:
                        args.ReturnValue = result;
                        break;
                }
            }
            catch (Exception ex)
            {

                if (Logger != null)
                    Logger.OnException(ex, this, args);
                lastError = ex;
                //em caso de resultado inexperado chama metodo para pegar conteudo padrão
                args.Proceed();
            }
        }
        private bool IsPrimitive(object obj)
        {
            if (obj == null)
                return false;

            var type = obj.GetType();

            return (type.IsPrimitive || type == typeof(Decimal) || type == typeof(String) || type == typeof(DateTime));
        }
        private byte[] Post(string url, Dictionary<string, object> values)
        {
            byte[] response = null;
            using (WebClient client = new WebClientWithTimeout(this.Timeout, this.KeepAlive))
            {

                if (XHttpRequestOverride)
                {
                    client.Headers.Add("X-HTTP-Method-Override:" + Method.ToString());
                    client.Headers.Add("X-HTTP-Method:" + Method.ToString());
                    client.Headers.Add("X-METHOD-OVERRIDE:" + Method.ToString());
                }

                if (!string.IsNullOrWhiteSpace(Header))
                {
                    var headers = Header.Split('\n');
                    foreach (var header in headers)
                    {
                        if (!string.IsNullOrWhiteSpace(header))
                            client.Headers.Add(header);
                    }
                }
                NameValueCollection formData = null;
                switch (ContentType)
                {
                    case HttpRequestContentType.JSON:
                        client.Headers.Add("Content-Type: application/json");
                        string json;
                        if (values.Count != 1 || IsPrimitive(values.First().Value))
                            json = Newtonsoft.Json.JsonConvert.SerializeObject(values);
                        else
                            json = Newtonsoft.Json.JsonConvert.SerializeObject(values.First());

                        response = client.UploadData(url, method: XHttpRequestOverride ? "POST" : Method.ToString(), data: Encoding.UTF8.GetBytes(json));
                        break;
                    case HttpRequestContentType.FormData:

                        formData = new NameValueCollection();

                        foreach (var value in values)
                            formData.Add(value.Key, (value.Value ?? string.Empty).ToString());
                        //upload values já seta content-type para application/x-www-form-urlencoded
                        response = client.UploadValues(url, method: XHttpRequestOverride ? "POST" : Method.ToString(), data: formData);
                        break;
                    case HttpRequestContentType.XML:
                        client.Headers.Add("Content-Type: application/xml");

                        if (values.Count != 1 || !IsPrimitive(values.First().Value))
                            throw new ArgumentException();

                        XmlSerializer ser = new XmlSerializer(values.First().Value.GetType());
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        System.IO.StringWriter writer = new System.IO.StringWriter(sb);
                        ser.Serialize(writer, values.First().Value);
                        response = client.UploadData(url, XHttpRequestOverride ? "POST" : Method.ToString(), Encoding.UTF8.GetBytes(values.First().Value as string));
                        break;
                    case HttpRequestContentType.Bytes:
                        client.Headers.Add("Content-Type: application/octet-stream");

                        if (values.Count != 1 || !(values.First().Value is byte[]))
                            throw new ArgumentException();

                        response = client.UploadData(url, XHttpRequestOverride ? "POST" : Method.ToString(), values.First().Value as byte[]);
                        break;
                    case HttpRequestContentType.String:
                        client.Headers.Add("Content-Type: text/plain;charset=utf-8");

                        if (values.Count != 1 || !(values.First().Value is byte[]))
                            throw new ArgumentException();

                        response = client.UploadData(url, XHttpRequestOverride ? "POST" : Method.ToString(), Encoding.UTF8.GetBytes(values.First().Value as string));
                        break;
                    default:
                        goto case HttpRequestContentType.JSON;
                }


            }
            return response;
        }
        private byte[] Get(string url, Dictionary<string, object> values)
        {
            if (values.Count > 0)
            {
                List<String> items = new List<String>();

                foreach (String name in values.Keys)
                    items.Add(String.Concat(name, "=", System.Web.HttpUtility.UrlEncode((values[name] ?? string.Empty).ToString())));

                string queryString = String.Join("&", items.ToArray());

                url += "?" + queryString;
            }
            byte[] response = null;
            using (WebClient client = new WebClientWithTimeout(this.Timeout, this.KeepAlive))
            {

                if (!string.IsNullOrWhiteSpace(Header))
                {
                    var headers = Header.Split('\n');
                    foreach (var header in headers)
                    {
                        if (!string.IsNullOrWhiteSpace(header))
                            client.Headers.Add(header);
                    }
                }
                response = client.DownloadData(url);
            }
            return response;
        }

        private class WebClientWithTimeout : WebClient
        {
            /// <summary>
            /// Time in milliseconds
            /// </summary>
            public int Timeout { get; set; }
            /// <summary>
            /// Set this property to true to send a Connection HTTP header with the value Keep-alive. 
            /// An application uses P:System.Net.HttpWebRequest.KeepAlive to indicate a preference for persistent connections. When the P:System.Net.HttpWebRequest.KeepAlive property is true,
            /// the application makes persistent connections to the servers that support them.
            /// Default value is true
            /// </summary>
            public bool KeepAlive { get; set; }

            public WebClientWithTimeout() : this(60000, true) { }

            public WebClientWithTimeout(int timeout, bool keepAlive)
            {
                this.Timeout = timeout;
                this.KeepAlive = keepAlive;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request != null)
                {
                    request.Timeout = this.Timeout;
                    (request as HttpWebRequest).KeepAlive = this.KeepAlive;
                }
                return request;
            }
        }
    }
}
