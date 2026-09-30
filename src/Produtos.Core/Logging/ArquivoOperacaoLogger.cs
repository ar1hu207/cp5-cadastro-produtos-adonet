using System.Diagnostics;
using System.Text;

namespace Produtos.Core.Logging;

/// <summary>
/// Grava cada operação como uma linha no arquivo de log:
/// 2026-09-30 14:03:22.114 | INFO  | INSERIR      | Produto Id=3 "Teclado" inserido.
/// </summary>
public class ArquivoOperacaoLogger : IOperacaoLogger
{
    private static readonly object Trava = new();

    public ArquivoOperacaoLogger(string caminhoArquivo)
    {
        CaminhoArquivo = Path.GetFullPath(caminhoArquivo);

        var pasta = Path.GetDirectoryName(CaminhoArquivo);
        if (!string.IsNullOrEmpty(pasta))
            Directory.CreateDirectory(pasta);
    }

    public string CaminhoArquivo { get; }

    public void Info(string operacao, string mensagem) => Escrever("INFO", operacao, mensagem);
    public void Aviso(string operacao, string mensagem) => Escrever("AVISO", operacao, mensagem);
    public void Erro(string operacao, string mensagem) => Escrever("ERRO", operacao, mensagem);

    private void Escrever(string nivel, string operacao, string mensagem)
    {
        var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {nivel,-5} | {operacao,-12} | {mensagem}";

        lock (Trava)
        {
            try
            {
                File.AppendAllText(CaminhoArquivo, linha + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Uma falha ao gravar o log não pode derrubar a operação principal.
                Debug.WriteLine($"Falha ao gravar log: {ex.Message}");
            }
        }
    }
}
