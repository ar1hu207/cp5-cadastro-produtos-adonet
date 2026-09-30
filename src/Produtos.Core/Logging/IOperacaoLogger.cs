namespace Produtos.Core.Logging;

/// <summary>
/// Registro das operações realizadas pela aplicação.
/// </summary>
public interface IOperacaoLogger
{
    void Info(string operacao, string mensagem);
    void Aviso(string operacao, string mensagem);
    void Erro(string operacao, string mensagem);
}
