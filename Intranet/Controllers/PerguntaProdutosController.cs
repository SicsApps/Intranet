using Intranet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Intranet.Controllers
{
    public class PerguntaProdutosController : Controller
    {
        private const int TamanhoMaximoPergunta = 2000;
        private static readonly HttpClient ClienteHttp = CriarClienteHttp();

        [HttpGet]
        public ActionResult Index()
        {
            ConfigurarRespostaUtf8();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Perguntar(PerguntaProdutosRequest request)
        {
            ConfigurarRespostaUtf8();

            var pergunta = request == null ? string.Empty : (request.Pergunta ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(pergunta))
            {
                return Erro(HttpStatusCode.BadRequest, "Digite uma pergunta antes de enviar.");
            }

            if (pergunta.Length > TamanhoMaximoPergunta)
            {
                return Erro(HttpStatusCode.BadRequest, "A pergunta deve ter no máximo 2.000 caracteres.");
            }

            Uri endpoint;
            if (!Uri.TryCreate(ConfigurationManager.AppSettings["SicsAnalytics:Endpoint"], UriKind.Absolute, out endpoint))
            {
                return Erro(HttpStatusCode.InternalServerError, "A integração com a SicsAnalytics não está configurada.");
            }

            var conteudoJson = JsonConvert.SerializeObject(new
            {
                mensagem = pergunta
            });

            using (var requisicao = new HttpRequestMessage(HttpMethod.Post, endpoint))
            using (var cancelamento = new CancellationTokenSource(TimeSpan.FromSeconds(ObterTimeoutSegundos())))
            {
                requisicao.Content = new StringContent(conteudoJson, Encoding.UTF8, "application/json");

                try
                {
                    using (var respostaApi = await ClienteHttp.SendAsync(requisicao, cancelamento.Token))
                    {
                        var conteudoResposta = await respostaApi.Content.ReadAsStringAsync();
                        var jsonResposta = TentarLerJson(conteudoResposta);

                        if (!respostaApi.IsSuccessStatusCode)
                        {
                            var mensagemApi = ObterTexto(jsonResposta, "mensagem");
                            return Erro(
                                HttpStatusCode.BadGateway,
                                string.IsNullOrWhiteSpace(mensagemApi)
                                    ? "Não foi possível obter uma resposta da SicsAnalytics."
                                    : mensagemApi);
                        }

                        var resposta = ObterTexto(jsonResposta, "resposta");
                        if (string.IsNullOrWhiteSpace(resposta))
                        {
                            return Erro(HttpStatusCode.BadGateway, "A SicsAnalytics retornou uma resposta vazia.");
                        }

                        var respostaHtml = RenderizarPartialParaString(
                            "_Resposta",
                            PerguntaProdutosRespostaParser.Criar(resposta));

                        return Json(new
                        {
                            success = true,
                            resposta,
                            respostaHtml
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    return Erro(HttpStatusCode.GatewayTimeout, "A SicsAnalytics demorou mais que o esperado para responder.");
                }
                catch (HttpRequestException)
                {
                    return Erro(HttpStatusCode.ServiceUnavailable, "A SicsAnalytics está indisponível no momento.");
                }
                catch (Exception)
                {
                    return Erro(HttpStatusCode.InternalServerError, "Ocorreu um erro ao processar a pergunta.");
                }
            }
        }

        private string RenderizarPartialParaString(string nomeView, object model)
        {
            ViewData.Model = model;

            using (var escritor = new StringWriter())
            {
                var resultadoView = ViewEngines.Engines.FindPartialView(ControllerContext, nomeView);

                if (resultadoView.View == null)
                {
                    throw new InvalidOperationException("A partial de resposta dos produtos nao foi encontrada.");
                }

                try
                {
                    var contextoView = new ViewContext(ControllerContext, resultadoView.View, ViewData, TempData, escritor);
                    resultadoView.View.Render(contextoView, escritor);
                    return escritor.GetStringBuilder().ToString();
                }
                finally
                {
                    resultadoView.ViewEngine.ReleaseView(ControllerContext, resultadoView.View);
                }
            }
        }

        private int ObterTimeoutSegundos()
        {
            int timeoutSegundos;
            if (!int.TryParse(ConfigurationManager.AppSettings["SicsAnalytics:TimeoutSeconds"], out timeoutSegundos))
            {
                return 120;
            }

            return Math.Max(5, Math.Min(timeoutSegundos, 300));
        }

        private static HttpClient CriarClienteHttp()
        {
            var cliente = new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            return cliente;
        }

        private void ConfigurarRespostaUtf8()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
        }

        private static JObject TentarLerJson(string conteudo)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
            {
                return null;
            }

            try
            {
                return JObject.Parse(conteudo);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string ObterTexto(JObject json, string propriedade)
        {
            var valor = json == null ? null : json.GetValue(propriedade, StringComparison.OrdinalIgnoreCase);
            return valor == null || valor.Type == JTokenType.Null ? null : valor.ToString();
        }

        private JsonResult Erro(HttpStatusCode statusCode, string mensagem)
        {
            Response.StatusCode = (int)statusCode;
            Response.TrySkipIisCustomErrors = true;

            return Json(new
            {
                success = false,
                mensagem
            });
        }
    }
}
