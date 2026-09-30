namespace Produtos.Core.Data;

/// <summary>
/// Erro de banco de dados já traduzido para uma mensagem que pode ser exibida
/// ao usuário. A exceção original do provider fica em <see cref="Exception.InnerException"/>.
/// </summary>
public class RepositorioException : Exception
{
    public RepositorioException(string operacao, string mensagem, Exception excecaoOriginal)
        : base(mensagem, excecaoOriginal)
    {
        Operacao = operacao;
    }

    public string Operacao { get; }
}
