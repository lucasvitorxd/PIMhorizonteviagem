using HorizonteViagens.Modelos;
using HorizonteViagens.Repositorio.Interfaces;

namespace HorizonteViagens.Negocio;

// Prepara o banco quando a Api ou a Web é iniciada com --preparar-banco:
// aplica as migrations, cadastra os combos de exemplo (só se não houver nenhum)
// e, com --acessos-teste, cria as contas de teste.
// Fala com o banco só pelos repositórios, como o resto do Negocio.
public static class Preparacao
{
    public static void Inicializar(
        IPortalRepositorio portal,
        IAdministradorRepositorio administradores,
        IClienteRepositorio clientes,
        bool acessosTeste,
        bool desenvolvimento,
        bool aplicarMigrations = true)
    {
        if (acessosTeste && !desenvolvimento)
            throw new InvalidOperationException("Acessos de teste só podem ser criados em Development.");

        // Os testes usam SQLite em memória, que já nasce com as tabelas:
        // por isso eles chamam com aplicarMigrations = false.
        if (aplicarMigrations)
            portal.AtualizarBanco();

        if (!portal.ExisteCombo())
            portal.AdicionarCombos(CombosDeExemplo());

        // O administrador original (admin@horizonteviagens.com) NÃO é mexido aqui:
        // ele continua funcionando como antes.

        if (acessosTeste)
            CriarAcessosDeTeste(administradores, clientes);
    }

    // Contas de teste sem data de validade (AcessoAte = null), para não
    // pararem de funcionar sozinhas no meio da apresentação.
    private static void CriarAcessosDeTeste(IAdministradorRepositorio administradores, IClienteRepositorio clientes)
    {
        var admin = administradores.ObterPorEmail("admin@horizonte.test");
        if (admin == null)
        {
            admin = new Administrador
            {
                Nome = "Administrador PIM",
                Email = "admin@horizonte.test",
                SenhaHash = SenhaHasher.Gerar("HorizontePim!2026"),
                Ativo = true,
                AcessoAte = null
            };
            administradores.Adicionar(admin);
        }
        else
        {
            admin.SenhaHash = SenhaHasher.Gerar("HorizontePim!2026");
            admin.Ativo = true;
            admin.AcessoAte = null;
            administradores.Atualizar(admin);
        }

        var cliente = clientes.ObterPorEmail("cliente@horizonte.test");
        if (cliente == null)
        {
            cliente = new Cliente
            {
                Nome = "Cliente PIM",
                Email = "cliente@horizonte.test",
                // CPF válido reservado para a conta de teste (diferente do usado nos exemplos do .http).
                Cpf = "11144477735",
                Celular = "11999999999",
                SenhaHash = SenhaHasher.Gerar("ViagemPim!2026"),
                Ativo = true,
                AcessoAte = null
            };
            clientes.Adicionar(cliente);
        }
        else
        {
            cliente.SenhaHash = SenhaHasher.Gerar("ViagemPim!2026");
            cliente.Ativo = true;
            cliente.AcessoAte = null;
            clientes.Atualizar(cliente);
        }
    }

    // Combos de exemplo: publicados, mas com preço ainda NÃO confirmado e sem vagas.
    // O administrador confirma preço, data de partida e vagas antes de vender.
    private static List<Combo> CombosDeExemplo() =>
    [
        Pacote("Paris", "Luxo", "Semana de Moda de Paris", "paris", 14, 17000, 3900, 1800,
            "Aéreo executivo ida e volta\nHotel categoria 5 estrelas, sujeito a cotação\nRoteiro de moda e jantar; desfiles oficiais dependem de convite\nCity tour e passeio no Sena"),
        Pacote("Tóquio", "Luxo", "Festival Hanami (Cherry Blossom)", "tokyo", 19, 29500, 2600, 5000,
            "Aéreo executivo ida e volta\nHotel categoria 5 estrelas, sujeito a cotação\nRoteiro de contemplação das cerejeiras e Monte Fuji\nOrçamento para transporte ferroviário e guia; confirmar o passe"),
        Pacote("Nova York", "Luxo", "Broadway + Times Square NYE", "newyork", 9, 19500, 4600, 2800,
            "Aéreo executivo ida e volta\nHotel categoria 4 estrelas\nBroadway e passeios sujeitos a cotação\nCelebração pública em Times Square é gratuita, sem lugar garantido"),
        Pacote("Porto Seguro", "Econômico", "Carnaval de Porto Seguro", "beach", 9, 1200, 450, 600,
            "Aéreo econômico ida e volta\nPousada, acomodação de até dois adultos\nProgramação de Carnaval a confirmar\nPasseios e serviços dependem de cotação"),
        Pacote("Ubatuba", "Econômico", "Festival de Surf de Ubatuba", "coast", 6, 300, 550, 350,
            "Transporte rodoviário ida e volta\nPousada, acomodação de até dois adultos\nProgramação de surfe a confirmar\nAula de surfe e caiaque sujeitos a cotação"),
        Pacote("Florianópolis", "Econômico", "Fenaostra — Festival da Ostra", "coast", 7, 850, 470, 500,
            "Aéreo econômico ida e volta\nHospedagem de categoria econômica\nProgramação da Fenaostra a confirmar\nGastronomia e passeio de barco sujeitos a cotação")
    ];

    private static Combo Pacote(string cidade, string categoria, string evento, string imagem,
        int noites, decimal transporte, decimal diaria, decimal experiencias, string inclusoes) => new()
    {
        Nome = cidade + " + experiências",
        Destino = cidade,
        Categoria = categoria,
        Evento = evento,
        Imagem = imagem,
        Noites = noites,
        Valor = transporte,
        DiariaQuarto = diaria,
        ValorExperiencias = experiencias,
        Inclusoes = inclusoes,
        Descricao = "Estimativa de planejamento baseada em referências públicas de novembro de 2026. " +
                    "Datas, hospedagem, ingresso e valor final dependem de confirmação. Consulte a composição do preço.",
        Publicado = true,
        PrecoConfirmado = false,
        Vagas = 0
    };
}
