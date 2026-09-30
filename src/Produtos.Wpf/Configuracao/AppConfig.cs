using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Produtos.Wpf.Configuracao;

/// <summary>
/// Lê o appsettings.json. Caminhos relativos são resolvidos a partir da pasta do executável,
/// para que o banco e o log não dependam da pasta de onde a aplicação foi aberta.
/// </summary>
public sealed record AppConfig(string ConnectionString, string CaminhoBanco, string CaminhoScript, string CaminhoLog)
{
    public static AppConfig Carregar()
    {
        var pastaBase = AppContext.BaseDirectory;

        var configuracao = new ConfigurationBuilder()
            .SetBasePath(pastaBase)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var connectionString = configuracao.GetConnectionString("ProdutosDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A connection string \"ProdutosDb\" não foi encontrada no appsettings.json.");

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.Combine(pastaBase, builder.DataSource);

        var script = configuracao["Banco:ScriptCriacao"] ?? "database/criar_tabela_produtos.sql";
        var log = configuracao["Log:Arquivo"] ?? "logs/operacoes.log";

        return new AppConfig(
            builder.ToString(),
            builder.DataSource,
            Path.Combine(pastaBase, script),
            Path.Combine(pastaBase, log));
    }
}
