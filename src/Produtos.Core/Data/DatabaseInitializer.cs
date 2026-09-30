using Microsoft.Data.Sqlite;
using Produtos.Core.Logging;

namespace Produtos.Core.Data;

/// <summary>
/// Garante que a tabela existe executando o script .sql entregue em /database.
/// O script usa CREATE TABLE IF NOT EXISTS, então rodar de novo não apaga dados.
/// </summary>
public static class DatabaseInitializer
{
    public static void Inicializar(string connectionString, string caminhoScript, IOperacaoLogger logger)
    {
        if (!File.Exists(caminhoScript))
        {
            logger.Erro("INICIALIZAR", $"Script de criação não encontrado: {caminhoScript}");
            throw new FileNotFoundException("Script de criação da tabela não encontrado.", caminhoScript);
        }

        var script = File.ReadAllText(caminhoScript);

        try
        {
            using var conexao = new SqliteConnection(connectionString);
            conexao.Open();

            using var comando = conexao.CreateCommand();
            comando.CommandText = script;
            comando.ExecuteNonQuery();

            logger.Info("INICIALIZAR", $"Banco pronto em {conexao.DataSource} (script {Path.GetFileName(caminhoScript)}).");
        }
        catch (SqliteException ex)
        {
            var mensagem = ProdutoRepository.TraduzirErro(ex);
            logger.Erro("INICIALIZAR", $"{mensagem} [SQLite {ex.SqliteErrorCode}] {ex.Message}");
            throw new RepositorioException("INICIALIZAR", mensagem, ex);
        }
    }
}
