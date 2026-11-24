namespace HorizonteViagens.Web.Models;

// Máscaras só para EXIBIR na tela. No banco, CPF e celular ficam só com os dígitos.
// Formatar é papel da camada de apresentação (Web), não do Negocio.
// É a mesma lógica do ToString(@"000\.000\.000\-00") da versão em console.
public static class Formatacao
{
    // 52998224725 vira 529.982.247-25. Se não tiver 11 números, mostra como está.
    public static string Cpf(string cpf) =>
        cpf.Length == 11 && long.TryParse(cpf, out long n)
            ? n.ToString(@"000\.000\.000\-00")
            : cpf;

    // 16988994455 vira (16) 9.8899-4455.
    public static string Celular(string celular) =>
        celular.Length == 11 && long.TryParse(celular, out long n)
            ? n.ToString(@"\(00\)\ 0\.0000\-0000")
            : celular;
}
