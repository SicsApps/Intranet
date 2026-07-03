using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Intranet.Models
{
    public class PerguntaProdutosRespostaViewModel
    {
        public PerguntaProdutosRespostaViewModel()
        {
            Blocos = new List<PerguntaProdutosRespostaBloco>();
        }

        public string TextoOriginal { get; set; }
        public IList<PerguntaProdutosRespostaBloco> Blocos { get; set; }
    }

    public class PerguntaProdutosRespostaBloco
    {
        public PerguntaProdutosRespostaBloco()
        {
            Itens = new List<PerguntaProdutosRespostaItem>();
        }

        public string Tipo { get; set; }
        public string Numero { get; set; }
        public string Texto { get; set; }
        public IList<PerguntaProdutosRespostaItem> Itens { get; set; }
    }

    public class PerguntaProdutosRespostaItem
    {
        public string Tipo { get; set; }
        public string Rotulo { get; set; }
        public string Valor { get; set; }
        public string Texto { get; set; }
    }

    public static class PerguntaProdutosRespostaParser
    {
        public static PerguntaProdutosRespostaViewModel Criar(string texto)
        {
            var modelo = new PerguntaProdutosRespostaViewModel
            {
                TextoOriginal = texto ?? string.Empty
            };

            if (TentarCriarPorJson(texto, modelo))
            {
                return modelo;
            }

            var produtoAtual = (PerguntaProdutosRespostaBloco)null;
            var listaAtual = (PerguntaProdutosRespostaBloco)null;
            var linhas = NormalizarQuebras(texto).Split('\n');

            foreach (var linha in linhas)
            {
                var conteudo = (linha ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(conteudo))
                {
                    listaAtual = null;
                    continue;
                }

                var produto = Regex.Match(conteudo, @"^(\d+)[.)]\s+(.+)$");
                if (produto.Success)
                {
                    PerguntaProdutosRespostaItem itemProduto;
                    var textoProduto = produto.Groups[2].Value;

                    if (TentarCriarItemRotulado(textoProduto, out itemProduto) && itemProduto.Tipo == "nome")
                    {
                        textoProduto = itemProduto.Valor;
                    }

                    produtoAtual = CriarProduto(modelo, textoProduto, produto.Groups[1].Value);

                    if (itemProduto != null && itemProduto.Tipo != "nome")
                    {
                        produtoAtual.Itens.Add(itemProduto);
                    }

                    listaAtual = null;
                    continue;
                }

                var produtoCodigoNome = Regex.Match(conteudo, @"^([A-Za-z0-9._/]*\d[A-Za-z0-9._/]*)\s*[-\u2013]\s*(.+)$");
                if (produtoCodigoNome.Success)
                {
                    produtoAtual = CriarProduto(modelo, produtoCodigoNome.Groups[2].Value, null);
                    produtoAtual.Itens.Add(new PerguntaProdutosRespostaItem
                    {
                        Tipo = "codigo",
                        Rotulo = "Código",
                        Valor = produtoCodigoNome.Groups[1].Value,
                        Texto = produtoCodigoNome.Groups[1].Value
                    });
                    listaAtual = null;
                    continue;
                }

                var cabecalhoProduto = Regex.Match(
                    conteudo,
                    @"^(?:produto|recomendacao|recomendação)\s*(\d+)?\s*[:.)-]\s*(.*)$",
                    RegexOptions.IgnoreCase);

                if (cabecalhoProduto.Success)
                {
                    var numeroProduto = cabecalhoProduto.Groups[1].Value;
                    var textoProduto = cabecalhoProduto.Groups[2].Value;

                    produtoAtual = CriarProduto(
                        modelo,
                        string.IsNullOrWhiteSpace(textoProduto) ? "Produto" : textoProduto,
                        string.IsNullOrWhiteSpace(numeroProduto) ? null : numeroProduto);
                    listaAtual = null;
                    continue;
                }

                var itemLista = Regex.Match(conteudo, @"^[-*\u2022]\s+(.+)$");
                if (itemLista.Success)
                {
                    var item = CriarItem(itemLista.Groups[1].Value);

                    if (item.Tipo == "nome")
                    {
                        produtoAtual = ResolverProdutoParaNome(modelo, produtoAtual, item.Valor);
                        listaAtual = null;
                        continue;
                    }

                    if (produtoAtual != null)
                    {
                        produtoAtual.Itens.Add(item);
                        continue;
                    }

                    if (listaAtual == null)
                    {
                        listaAtual = CriarBloco("lista", string.Empty);
                        modelo.Blocos.Add(listaAtual);
                    }

                    listaAtual.Itens.Add(item);
                    continue;
                }

                PerguntaProdutosRespostaItem itemRotulado;
                if (TentarCriarItemRotulado(conteudo, out itemRotulado))
                {
                    if (itemRotulado.Tipo == "nome")
                    {
                        produtoAtual = ResolverProdutoParaNome(modelo, produtoAtual, itemRotulado.Valor);
                        listaAtual = null;
                        continue;
                    }

                    if (itemRotulado.Tipo == "codigo" && produtoAtual == null)
                    {
                        produtoAtual = CriarProduto(modelo, string.Empty, null);
                    }

                    if (produtoAtual != null)
                    {
                        produtoAtual.Itens.Add(itemRotulado);
                        listaAtual = null;
                        continue;
                    }

                    if (listaAtual == null)
                    {
                        listaAtual = CriarBloco("lista", string.Empty);
                        modelo.Blocos.Add(listaAtual);
                    }

                    listaAtual.Itens.Add(itemRotulado);
                    continue;
                }

                var titulo = Regex.Match(conteudo, @"^#{1,3}\s+(.+)$");
                if (titulo.Success)
                {
                    produtoAtual = null;
                    listaAtual = null;
                    modelo.Blocos.Add(CriarBloco("titulo", titulo.Groups[1].Value));
                    continue;
                }

                listaAtual = null;

                if (produtoAtual != null)
                {
                    produtoAtual.Itens.Add(new PerguntaProdutosRespostaItem
                    {
                        Tipo = "texto",
                        Texto = conteudo,
                        Valor = conteudo
                    });
                    continue;
                }

                modelo.Blocos.Add(CriarBloco("paragrafo", conteudo));
            }

            return modelo;
        }

        public static string FormatarInline(string texto)
        {
            var valor = HttpUtility.HtmlEncode(FormatarDatas(texto ?? string.Empty));

            valor = Regex.Replace(valor, @"`([^`]+)`", "<code>$1</code>");
            valor = Regex.Replace(valor, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
            valor = Regex.Replace(valor, @"__([^_]+)__", "<strong>$1</strong>");
            valor = Regex.Replace(valor, @"\*([^*]+)\*", "<strong>$1</strong>");

            return valor.Replace("*", string.Empty);
        }

        private static PerguntaProdutosRespostaBloco CriarBloco(string tipo, string texto)
        {
            return new PerguntaProdutosRespostaBloco
            {
                Tipo = tipo,
                Texto = texto
            };
        }

        private static bool TentarCriarPorJson(string texto, PerguntaProdutosRespostaViewModel modelo)
        {
            var json = ExtrairJson(texto);

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var token = JToken.Parse(json);

                if (token.Type == JTokenType.Array)
                {
                    foreach (var item in token.Children())
                    {
                        AdicionarProdutoJson(modelo, item);
                    }

                    return modelo.Blocos.Count > 0;
                }

                if (token.Type == JTokenType.Object)
                {
                    var objeto = (JObject)token;
                    var produtos = ObterArray(objeto, "produtos", "itens", "recomendacoes", "recomendações", "resposta");

                    if (produtos != null)
                    {
                        foreach (var item in produtos.Children())
                        {
                            AdicionarProdutoJson(modelo, item);
                        }

                        return modelo.Blocos.Count > 0;
                    }

                    return AdicionarProdutoJson(modelo, objeto);
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        private static bool AdicionarProdutoJson(PerguntaProdutosRespostaViewModel modelo, JToken token)
        {
            var objeto = token as JObject;

            if (objeto == null)
            {
                return false;
            }

            var nome = ObterValor(objeto, "nome", "nomprod", "nomeprod", "nome produto", "nome do produto", "produto");
            var codigo = ObterValor(objeto, "codigo", "código", "codprod", "codigo produto", "codigo do produto");

            if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(codigo))
            {
                return false;
            }

            var produto = CriarProduto(modelo, nome, null);

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                produto.Itens.Add(CriarItemJson("codigo", "Código", codigo));
            }

            AdicionarItemJsonSeExistir(produto, objeto, "meta", "Linha e marca", "linha e marca", "linha/marca", "marca e linha", "marca/linha");
            AdicionarItemJsonSeExistir(produto, objeto, "meta", "Linha", "linha", "grupo", "categoria", "familia");
            AdicionarItemJsonSeExistir(produto, objeto, "meta", "Marca", "marca");
            AdicionarItemJsonSeExistir(produto, objeto, "justificativa", "Justificativa", "justificativa", "motivo", "recomendacao", "recomendação");

            foreach (var propriedade in objeto.Properties())
            {
                var nomeNormalizado = NormalizarTexto(propriedade.Name);

                if (EhPropriedadeConhecidaJson(nomeNormalizado) ||
                    propriedade.Value == null ||
                    propriedade.Value.Type == JTokenType.Null ||
                    propriedade.Value.Type == JTokenType.Array ||
                    propriedade.Value.Type == JTokenType.Object)
                {
                    continue;
                }

                var valor = propriedade.Value.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(valor))
                {
                    produto.Itens.Add(CriarItemJson("padrao", propriedade.Name, valor));
                }
            }

            return true;
        }

        private static PerguntaProdutosRespostaItem CriarItemJson(string tipo, string rotulo, string valor)
        {
            return new PerguntaProdutosRespostaItem
            {
                Tipo = tipo,
                Rotulo = rotulo,
                Valor = valor,
                Texto = string.IsNullOrWhiteSpace(rotulo) ? valor : rotulo + ": " + valor
            };
        }

        private static void AdicionarItemJsonSeExistir(
            PerguntaProdutosRespostaBloco produto,
            JObject objeto,
            string tipo,
            string rotulo,
            params string[] nomes)
        {
            var valor = ObterValor(objeto, nomes);

            if (!string.IsNullOrWhiteSpace(valor))
            {
                produto.Itens.Add(CriarItemJson(tipo, rotulo, valor));
            }
        }

        private static string ObterValor(JObject objeto, params string[] nomes)
        {
            foreach (var propriedade in objeto.Properties())
            {
                var nomeNormalizado = NormalizarTexto(propriedade.Name);

                foreach (var nome in nomes)
                {
                    if (nomeNormalizado == NormalizarTexto(nome))
                    {
                        return propriedade.Value == null || propriedade.Value.Type == JTokenType.Null
                            ? null
                            : propriedade.Value.ToString().Trim();
                    }
                }
            }

            return null;
        }

        private static JArray ObterArray(JObject objeto, params string[] nomes)
        {
            foreach (var propriedade in objeto.Properties())
            {
                var nomeNormalizado = NormalizarTexto(propriedade.Name);

                foreach (var nome in nomes)
                {
                    if (nomeNormalizado == NormalizarTexto(nome))
                    {
                        return propriedade.Value as JArray;
                    }
                }
            }

            return null;
        }

        private static bool EhPropriedadeConhecidaJson(string nomeNormalizado)
        {
            return nomeNormalizado == "nome" ||
                nomeNormalizado == "nomprod" ||
                nomeNormalizado == "nomeprod" ||
                nomeNormalizado == "nome produto" ||
                nomeNormalizado == "nome do produto" ||
                nomeNormalizado == "produto" ||
                nomeNormalizado == "codigo" ||
                nomeNormalizado == "codprod" ||
                nomeNormalizado == "codigo produto" ||
                nomeNormalizado == "codigo do produto" ||
                nomeNormalizado == "linha e marca" ||
                nomeNormalizado == "linha/marca" ||
                nomeNormalizado == "marca e linha" ||
                nomeNormalizado == "marca/linha" ||
                nomeNormalizado == "linha" ||
                nomeNormalizado == "grupo" ||
                nomeNormalizado == "categoria" ||
                nomeNormalizado == "familia" ||
                nomeNormalizado == "marca" ||
                nomeNormalizado == "justificativa" ||
                nomeNormalizado == "motivo" ||
                nomeNormalizado == "recomendacao";
        }

        private static string ExtrairJson(string texto)
        {
            var valor = (texto ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            valor = Regex.Replace(valor, @"^\s*```(?:json)?\s*", string.Empty, RegexOptions.IgnoreCase);
            valor = Regex.Replace(valor, @"\s*```\s*$", string.Empty);
            valor = valor.Trim();

            var indiceArray = valor.IndexOf('[');
            var indiceObjeto = valor.IndexOf('{');
            var indiceInicio = -1;

            if (indiceArray >= 0 && indiceObjeto >= 0)
            {
                indiceInicio = Math.Min(indiceArray, indiceObjeto);
            }
            else
            {
                indiceInicio = Math.Max(indiceArray, indiceObjeto);
            }

            if (indiceInicio < 0)
            {
                return null;
            }

            valor = valor.Substring(indiceInicio).Trim();

            if (valor.StartsWith("["))
            {
                var fimArray = valor.LastIndexOf(']');
                return fimArray >= 0 ? valor.Substring(0, fimArray + 1) : valor;
            }

            if (valor.StartsWith("{"))
            {
                var fimObjeto = valor.LastIndexOf('}');
                return fimObjeto >= 0 ? valor.Substring(0, fimObjeto + 1) : valor;
            }

            return null;
        }

        private static PerguntaProdutosRespostaBloco CriarProduto(
            PerguntaProdutosRespostaViewModel modelo,
            string texto,
            string numero)
        {
            var produto = CriarBloco("produto", texto);
            produto.Numero = string.IsNullOrWhiteSpace(numero)
                ? ObterProximoNumeroProduto(modelo)
                : numero;

            modelo.Blocos.Add(produto);
            return produto;
        }

        private static PerguntaProdutosRespostaBloco ResolverProdutoParaNome(
            PerguntaProdutosRespostaViewModel modelo,
            PerguntaProdutosRespostaBloco produtoAtual,
            string nomeProduto)
        {
            if (produtoAtual == null || ProdutoJaTemNome(produtoAtual))
            {
                return CriarProduto(modelo, nomeProduto, null);
            }

            produtoAtual.Texto = nomeProduto;
            return produtoAtual;
        }

        private static bool ProdutoJaTemNome(PerguntaProdutosRespostaBloco produto)
        {
            var texto = NormalizarTexto(produto == null ? null : produto.Texto);
            return !string.IsNullOrWhiteSpace(texto) &&
                texto != "produto" &&
                !texto.StartsWith("produto ");
        }

        private static bool TentarCriarItemRotulado(string texto, out PerguntaProdutosRespostaItem item)
        {
            item = null;

            var textoSemMarcacao = RemoverMarcacoes(texto);
            var indiceSeparador = textoSemMarcacao.IndexOf(':');

            if (indiceSeparador <= 0 || indiceSeparador > 60)
            {
                return false;
            }

            item = CriarItem(texto);
            return !string.IsNullOrWhiteSpace(item.Rotulo);
        }

        private static PerguntaProdutosRespostaItem CriarItem(string texto)
        {
            var textoSemMarcacao = RemoverMarcacoes(texto);
            var indiceSeparador = textoSemMarcacao.IndexOf(':');
            var item = new PerguntaProdutosRespostaItem
            {
                Texto = texto,
                Valor = textoSemMarcacao,
                Tipo = "padrao"
            };

            if (indiceSeparador > 0 && indiceSeparador <= 60)
            {
                item.Rotulo = textoSemMarcacao.Substring(0, indiceSeparador).Trim();
                item.Valor = textoSemMarcacao.Substring(indiceSeparador + 1).Trim();
                item.Tipo = ObterTipoItem(item.Rotulo);
            }

            return item;
        }

        private static string ObterTipoItem(string rotulo)
        {
            var normalizado = NormalizarTexto(rotulo);

            if (normalizado == "codigo" ||
                normalizado == "codprod" ||
                normalizado == "codigo produto" ||
                normalizado == "codigo do produto")
            {
                return "codigo";
            }

            if (normalizado == "nome" ||
                normalizado == "produto" ||
                normalizado == "nomprod" ||
                normalizado == "nomeprod" ||
                normalizado == "nome produto" ||
                normalizado == "nome do produto")
            {
                return "nome";
            }

            if (normalizado.Contains("justificativa") ||
                normalizado.Contains("motivo") ||
                normalizado.Contains("recomendacao") ||
                normalizado.StartsWith("por que"))
            {
                return "justificativa";
            }

            if (normalizado == "linha" ||
                normalizado == "linha e marca" ||
                normalizado == "linha/marca" ||
                normalizado == "marca e linha" ||
                normalizado == "marca/linha" ||
                normalizado == "marca" ||
                normalizado == "categoria" ||
                normalizado == "familia" ||
                normalizado == "grupo" ||
                normalizado == "data de lancamento" ||
                normalizado == "lancamento")
            {
                return "meta";
            }

            return "padrao";
        }

        private static string NormalizarQuebras(string texto)
        {
            return (texto ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        private static string FormatarDatas(string texto)
        {
            return Regex.Replace(
                texto,
                @"\b(\d{4})-(\d{2})-(\d{2})T00:00:00\b",
                "$3/$2/$1");
        }

        private static string RemoverMarcacoes(string texto)
        {
            return (texto ?? string.Empty)
                .Replace("**", string.Empty)
                .Replace("__", string.Empty)
                .Replace("`", string.Empty)
                .Replace("*", string.Empty)
                .Trim();
        }

        private static string NormalizarTexto(string texto)
        {
            var semAcento = new StringBuilder();
            var textoNormalizado = (texto ?? string.Empty).Normalize(NormalizationForm.FormD);

            foreach (var caractere in textoNormalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                {
                    semAcento.Append(caractere);
                }
            }

            return Regex.Replace(semAcento.ToString().ToLowerInvariant(), @"\s+", " ").Trim();
        }

        private static string ObterProximoNumeroProduto(PerguntaProdutosRespostaViewModel modelo)
        {
            var totalProdutos = 0;

            foreach (var bloco in modelo.Blocos)
            {
                if (bloco.Tipo == "produto")
                {
                    totalProdutos++;
                }
            }

            return (totalProdutos + 1).ToString();
        }
    }
}
