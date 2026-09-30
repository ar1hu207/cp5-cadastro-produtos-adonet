using Microsoft.Data.Sqlite;
using Produtos.Core.Data;
using Produtos.Core.Logging;

namespace Produtos.Tests;

/// <summary>
/// Cria um arquivo SQLite temporário por teste, com a tabela criada pelo script real,
/// e apaga tudo ao final.
/// </summary>
public sealed class BancoDeTeste : IDisposable
{
    private readonly string _pasta;

    public BancoDeTeste(bool criarTabela = true)
    {
        _pasta = Path.Combine(Path.GetTempPath(), "produtos-testes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);

        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_pasta, "produtos.db"),
            Pooling = false
        }.ToString();

        Logger = new ArquivoOperacaoLogger(Path.Combine(_pasta, "operacoes.log"));

        if (criarTabela)
        {
            var script = Path.Combine(AppContext.BaseDirectory, "criar_tabela_produtos.sql");
            DatabaseInitializer.Inicializar(ConnectionString, script, Logger);
        }

        Repositorio = new ProdutoRepository(ConnectionString, Logger);
    }

    public string ConnectionString { get; }
    public ArquivoOperacaoLogger Logger { get; }
    public ProdutoRepository Repositorio { get; }

    public string[] LinhasDoLog() =>
        File.Exists(Logger.CaminhoArquivo) ? File.ReadAllLines(Logger.CaminhoArquivo) : [];

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_pasta, recursive: true); }
        catch (IOException) { /* arquivo temporário; o SO limpa depois */ }
    }
}
