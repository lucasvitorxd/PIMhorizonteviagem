namespace HorizonteViagens.Modelos;

// A agência: quem tem acesso exclusivo aos dados dos clientes.
// HERANÇA: Nome, Email, SenhaHash e Ativo vêm da classe mãe Usuario.
public class Administrador : Usuario
{
    // Atualizado a cada login. Ajuda a saber quem mexeu no sistema e quando.
    public DateTime? UltimoAcesso { get; set; }

    // Resposta do polimorfismo: para o login, este usuário é um "Administrador".
    public override string Perfil => "Administrador";
}
