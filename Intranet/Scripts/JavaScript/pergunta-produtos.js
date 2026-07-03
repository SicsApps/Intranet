document.addEventListener('DOMContentLoaded', function () {
    var form = document.getElementById('form-pergunta-produtos');

    if (!form) {
        return;
    }

    var textarea = document.getElementById('pergunta-produtos');
    var contador = document.getElementById('contador-pergunta-produtos');
    var botao = document.getElementById('botao-pergunta-produtos');
    var textoBotao = botao.querySelector('.texto-botao');
    var carregandoBotao = botao.querySelector('.carregando-botao');
    var carregamento = document.getElementById('carregamento-pergunta-produtos');
    var erro = document.getElementById('erro-pergunta-produtos');
    var resposta = document.getElementById('resposta-pergunta-produtos');
    var textoResposta = document.getElementById('texto-resposta-produtos');
    var token = form.querySelector('input[name="__RequestVerificationToken"]');

    textarea.addEventListener('input', function () {
        contador.textContent = textarea.value.length;
        ocultarErro();
    });

    form.addEventListener('submit', function (event) {
        event.preventDefault();

        var pergunta = textarea.value.trim();
        if (!pergunta) {
            exibirErro('Digite uma pergunta antes de enviar.');
            textarea.focus();
            return;
        }

        enviarPergunta(pergunta);
    });

    async function enviarPergunta(pergunta) {
        definirCarregamento(true);
        ocultarErro();
        resposta.classList.add('d-none');
        textoResposta.textContent = '';

        var dados = new URLSearchParams();
        dados.append('__RequestVerificationToken', token.value);
        dados.append('Pergunta', pergunta);

        try {
            var retorno = await fetch(form.action, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: dados.toString()
            });

            var resultado = await lerJson(retorno);

            if (!retorno.ok || !resultado.success) {
                throw new Error(resultado.mensagem || 'Não foi possível processar sua pergunta.');
            }

            exibirResposta(resultado);
            resposta.classList.remove('d-none');
            resposta.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } catch (falha) {
            exibirErro(falha.message || 'Não foi possível processar sua pergunta.');
        } finally {
            definirCarregamento(false);
        }
    }

    async function lerJson(retorno) {
        try {
            return await retorno.json();
        } catch (falha) {
            return {
                success: false,
                mensagem: 'O servidor retornou uma resposta inválida.'
            };
        }
    }

    function definirCarregamento(ativo) {
        botao.disabled = ativo;
        textarea.disabled = ativo;
        textoBotao.classList.toggle('d-none', ativo);
        carregandoBotao.classList.toggle('d-none', !ativo);
        carregamento.classList.toggle('d-none', !ativo);
    }

    function exibirErro(mensagem) {
        erro.textContent = mensagem;
        erro.classList.remove('d-none');
    }

    function ocultarErro() {
        erro.textContent = '';
        erro.classList.add('d-none');
    }

    function exibirResposta(resultado) {
        if (resultado.respostaHtml) {
            textoResposta.innerHTML = resultado.respostaHtml;
            return;
        }

        textoResposta.textContent = resultado.resposta || '';
    }
});
