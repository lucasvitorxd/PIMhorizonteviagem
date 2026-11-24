using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Interfaces;

namespace HorizonteViagens.Negocio;

// Token da conta de TESTE do Mercado Pago e endereço de retorno (opcional).
// Os dois vêm da configuração (User Secrets), nunca do código.
public record PagamentoConfiguracao(string Token, string Retorno);

// Pagamento pelo Mercado Pago, só em ambiente de teste (sandbox).
// O sistema nunca recebe número de cartão nem chave Pix: o cliente paga
// na página do Mercado Pago e depois a reserva consulta o resultado.
// Sem token configurado, o resto do sistema funciona normalmente;
// só o botão de pagar mostra que o pagamento está indisponível.
public class PagamentoNegocio
{
    private readonly HttpClient _http;
    private readonly PagamentoConfiguracao _configuracao;
    private readonly PortalNegocio _portal;
    private readonly IPortalRepositorio _repositorio;

    public PagamentoNegocio(HttpClient http, PagamentoConfiguracao configuracao, PortalNegocio portal, IPortalRepositorio repositorio)
    {
        _http = http;
        _configuracao = configuracao;
        _portal = portal;
        _repositorio = repositorio;
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_configuracao.Token);

    // Gera o link de pagamento da reserva na página de testes do Mercado Pago.
    public async Task<string> Iniciar(int compraId, int clienteId)
    {
        var compra = _portal.Reserva(compraId, clienteId);

        if (compra.Status != "Pendente" || compra.ExpiraEm <= DateTime.UtcNow)
            throw new RegraDeNegocioException("Esta reserva não está disponível para pagamento.");

        if (compra.PagamentoUrl != null)
            return compra.PagamentoUrl;

        // Se alguém configurar por engano o token de uma conta real, o sistema recusa.
        var usuario = await Chamar(HttpMethod.Get, "/users/me");
        var contaDeTeste = usuario.TryGetProperty("tags", out var tags) &&
                           tags.EnumerateArray().Any(t => t.GetString() == "test_user");
        if (!contaDeTeste)
            throw new RegraDeNegocioException("Use somente uma conta vendedora de teste do Mercado Pago.");

        var preferencia = new Dictionary<string, object>
        {
            ["external_reference"] = Referencia(compra),
            ["items"] = new[]
            {
                new
                {
                    id = compra.ComboId.ToString(),
                    title = compra.NomePacote,
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = compra.Valor
                }
            },
            ["expires"] = false
        };

        // O retorno é opcional: no uso local o cliente volta à reserva e clica em "verificar".
        if (!string.IsNullOrWhiteSpace(_configuracao.Retorno))
        {
            if (!Uri.TryCreate(_configuracao.Retorno, UriKind.Absolute, out var retorno) || retorno.Scheme != "https")
                throw new RegraDeNegocioException("O retorno opcional precisa usar HTTPS.");

            preferencia["back_urls"] = new
            {
                success = _configuracao.Retorno,
                pending = _configuracao.Retorno,
                failure = _configuracao.Retorno
            };
        }

        var resposta = await Chamar(HttpMethod.Post, "/checkout/preferences", preferencia);
        var link = resposta.GetProperty("sandbox_init_point").GetString();

        // Só aceita link que realmente seja do Mercado Pago.
        if (!LinkDoMercadoPago(link))
            throw new RegraDeNegocioException("Link de pagamento inválido.");

        if (compra.ExpiraEm <= DateTime.UtcNow)
            throw new RegraDeNegocioException("A reserva expirou. Faça uma nova reserva.");

        compra.PreferenciaId = resposta.GetProperty("id").GetString();
        compra.PagamentoUrl = link;
        _repositorio.Salvar();
        return link!;
    }

    // Pergunta ao Mercado Pago se já existe pagamento aprovado para a reserva.
    public async Task<Compra> Verificar(int compraId, int clienteId)
    {
        var compra = _portal.Reserva(compraId, clienteId);
        var referencia = Referencia(compra);

        var resposta = await Chamar(HttpMethod.Get,
            "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" + Uri.EscapeDataString(referencia));

        foreach (var pagamento in resposta.GetProperty("results").EnumerateArray())
        {
            var status = pagamento.GetProperty("status").GetString()!;
            if (status is not ("approved" or "refunded" or "charged_back"))
                continue;

            _repositorio.AplicarPagamento(
                compra.Id,
                pagamento.GetProperty("id").ToString(),
                status,
                pagamento.GetProperty("transaction_amount").GetDecimal(),
                pagamento.GetProperty("external_reference").GetString()!,
                teste: !pagamento.GetProperty("live_mode").GetBoolean());
        }

        return _portal.Reserva(compraId, clienteId);
    }

    // Código que liga o pagamento à reserva (ex.: HV-15-3f2a...).
    private static string Referencia(Compra compra) => $"HV-{compra.Id}-{compra.RequisicaoId:N}";

    private static bool LinkDoMercadoPago(string? link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var endereco) &&
        endereco.Scheme == "https" &&
        (endereco.Host.Equals("mercadopago.com.br", StringComparison.OrdinalIgnoreCase) ||
         endereco.Host.EndsWith(".mercadopago.com.br", StringComparison.OrdinalIgnoreCase));

    private async Task<JsonElement> Chamar(HttpMethod metodo, string caminho, object? corpo = null)
    {
        if (!Configurado)
            throw new RegraDeNegocioException("Pagamento indisponível. Configure a conta de testes do Mercado Pago.");

        using var requisicao = new HttpRequestMessage(metodo, "https://api.mercadopago.com" + caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracao.Token);

        if (corpo != null)
            requisicao.Content = JsonContent.Create(corpo);

        using var resposta = await _http.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
        {
            var detalhe = await resposta.Content.ReadAsStringAsync();

            Console.WriteLine($"ERRO MERCADO PAGO - HTTP {(int)resposta.StatusCode}");
            Console.WriteLine($"DETALHES: {detalhe}");

            throw new RegraDeNegocioException(
                "O serviço de pagamento não concluiu a solicitação. Tente novamente!");
        }

        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }
}
