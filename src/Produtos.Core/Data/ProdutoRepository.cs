using Microsoft.Data.Sqlite;
using Produtos.Core.Logging;
using Produtos.Core.Models;

namespace Produtos.Core.Data;

/// <summary>
/// Único ponto de acesso à tabela Produtos. Todo SQL da aplicação fica aqui,
/// sempre com parâmetros (@Nome, @Id, ...) e nunca concatenando dados do usuário.
/// </summary>
public class ProdutoRepository : IProdutoRepository
{
    // Constante de compilação: a lista de colunas nunca vem do usuário.
    private const string Colunas = "Id, Nome, Preco, Estoque, Categoria, Ativo, DataCadastro, DataExclusao";

    private readonly string _connectionString;
    private readonly IOperacaoLogger _logger;

    public ProdutoRepository(string connectionString, IOperacaoLogger logger)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string não pode ser vazia.", nameof(connectionString));

        _connectionString = connectionString;
        _logger = logger;
    }

    // ------------------------------------------------------------------ INSERT

    public int Inserir(Produto produto)
    {
        ValidarProduto(produto);

        const string sqlInsert =
            "INSERT INTO Produtos (Nome, Preco, Estoque, Categoria) " +
            "VALUES (@Nome, @Preco, @Estoque, @Categoria);";

        return Executar("INSERIR", () =>
        {
            using var conexao = AbrirConexao();

            // O INSERT e a leitura do Id gerado ficam na mesma transação.
            // Se algo falhar antes do Commit, o Dispose faz o rollback.
            using var transacao = conexao.BeginTransaction();

            using (var comando = conexao.CreateCommand())
            {
                comando.Transaction = transacao;
                comando.CommandText = sqlInsert;
                AdicionarParametrosDeDados(comando, produto);
                comando.ExecuteNonQuery();
            }

            int novoId;
            using (var comando = conexao.CreateCommand())
            {
                comando.Transaction = transacao;
                comando.CommandText = "SELECT last_insert_rowid();";
                using var reader = comando.ExecuteReader();
                reader.Read();
                novoId = reader.GetInt32(0);
            }

            transacao.Commit();

            produto.Id = novoId;
            _logger.Info("INSERIR", $"Produto Id={novoId} \"{produto.Nome}\" inserido.");
            return novoId;
        });
    }

    // ------------------------------------------------------------------ SELECT

    public List<Produto> Listar(bool incluirExcluidos = false)
    {
        const string sql =
            "SELECT " + Colunas + " FROM Produtos " +
            "WHERE (@IncluirExcluidos = 1 OR Ativo = 1) " +
            "ORDER BY Id;";

        return Executar("LISTAR", () =>
        {
            using var conexao = AbrirConexao();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.Add("@IncluirExcluidos", SqliteType.Integer).Value = incluirExcluidos ? 1 : 0;

            var produtos = new List<Produto>();
            using var reader = comando.ExecuteReader();
            while (reader.Read())
                produtos.Add(Mapear(reader));

            _logger.Info("LISTAR", $"{produtos.Count} produto(s) listado(s){(incluirExcluidos ? " (incluindo excluídos)" : "")}.");
            return produtos;
        });
    }

    public Produto? BuscarPorId(int id, bool incluirExcluidos = false)
    {
        ValidarId(id);

        const string sql =
            "SELECT " + Colunas + " FROM Produtos " +
            "WHERE Id = @Id AND (@IncluirExcluidos = 1 OR Ativo = 1);";

        return Executar("BUSCAR", () =>
        {
            using var conexao = AbrirConexao();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.Add("@Id", SqliteType.Integer).Value = id;
            comando.Parameters.Add("@IncluirExcluidos", SqliteType.Integer).Value = incluirExcluidos ? 1 : 0;

            using var reader = comando.ExecuteReader();
            var produto = reader.Read() ? Mapear(reader) : null;

            if (produto is null)
                _logger.Aviso("BUSCAR", $"Produto Id={id} não encontrado.");
            else
                _logger.Info("BUSCAR", $"Produto Id={id} \"{produto.Nome}\" encontrado.");

            return produto;
        });
    }

    // ------------------------------------------------------------------ UPDATE

    public bool Atualizar(Produto produto)
    {
        ValidarId(produto.Id);
        ValidarProduto(produto);

        const string sql =
            "UPDATE Produtos SET Nome = @Nome, Preco = @Preco, Estoque = @Estoque, Categoria = @Categoria " +
            "WHERE Id = @Id AND Ativo = 1;";

        var atualizado = ExecutarEscritaDeUmaLinha("ATUALIZAR", sql, comando =>
        {
            AdicionarParametrosDeDados(comando, produto);
            comando.Parameters.Add("@Id", SqliteType.Integer).Value = produto.Id;
        });

        if (atualizado)
            _logger.Info("ATUALIZAR", $"Produto Id={produto.Id} atualizado para \"{produto.Nome}\", {produto.Preco:0.00}, estoque {produto.Estoque}, {produto.Categoria}.");
        else
            _logger.Aviso("ATUALIZAR", $"Produto Id={produto.Id} não encontrado ou excluído; nada foi alterado.");

        return atualizado;
    }

    // ------------------------------------------------------------------ DELETE

    public bool Excluir(int id)
    {
        ValidarId(id);

        const string sql =
            "UPDATE Produtos SET Ativo = 0, DataExclusao = datetime('now', 'localtime') " +
            "WHERE Id = @Id AND Ativo = 1;";

        var excluido = ExecutarEscritaDeUmaLinha("EXCLUIR", sql,
            comando => comando.Parameters.Add("@Id", SqliteType.Integer).Value = id);

        if (excluido)
            _logger.Info("EXCLUIR", $"Produto Id={id} marcado como excluído (soft delete).");
        else
            _logger.Aviso("EXCLUIR", $"Produto Id={id} não encontrado ou já excluído.");

        return excluido;
    }

    public bool Restaurar(int id)
    {
        ValidarId(id);

        const string sql =
            "UPDATE Produtos SET Ativo = 1, DataExclusao = NULL " +
            "WHERE Id = @Id AND Ativo = 0;";

        var restaurado = ExecutarEscritaDeUmaLinha("RESTAURAR", sql,
            comando => comando.Parameters.Add("@Id", SqliteType.Integer).Value = id);

        if (restaurado)
            _logger.Info("RESTAURAR", $"Produto Id={id} restaurado.");
        else
            _logger.Aviso("RESTAURAR", $"Produto Id={id} não encontrado entre os excluídos.");

        return restaurado;
    }

    public bool ExcluirPermanentemente(int id)
    {
        ValidarId(id);

        const string sql = "DELETE FROM Produtos WHERE Id = @Id;";

        var removido = ExecutarEscritaDeUmaLinha("EXCLUIR_DEF", sql,
            comando => comando.Parameters.Add("@Id", SqliteType.Integer).Value = id);

        if (removido)
            _logger.Info("EXCLUIR_DEF", $"Produto Id={id} removido definitivamente (DELETE).");
        else
            _logger.Aviso("EXCLUIR_DEF", $"Produto Id={id} não encontrado.");

        return removido;
    }

    // ------------------------------------------------------------------ Apoio

    private SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection(_connectionString);
        conexao.Open();
        return conexao;
    }

    /// <summary>
    /// Executa um INSERT/UPDATE/DELETE que deve afetar exatamente uma linha (sempre filtramos por Id).
    /// Roda dentro de uma transação: se o número de linhas afetadas não for 1, nada é gravado.
    /// </summary>
    private bool ExecutarEscritaDeUmaLinha(string operacao, string sql, Action<SqliteCommand> adicionarParametros)
    {
        return Executar(operacao, () =>
        {
            using var conexao = AbrirConexao();
            using var transacao = conexao.BeginTransaction();
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = sql;
            adicionarParametros(comando);

            var linhasAfetadas = comando.ExecuteNonQuery();

            if (linhasAfetadas != 1)
            {
                transacao.Rollback();
                return false;
            }

            transacao.Commit();
            return true;
        });
    }

    private static void AdicionarParametrosDeDados(SqliteCommand comando, Produto produto)
    {
        comando.Parameters.Add("@Nome", SqliteType.Text).Value = produto.Nome.Trim();
        comando.Parameters.Add("@Preco", SqliteType.Real).Value = produto.Preco;
        comando.Parameters.Add("@Estoque", SqliteType.Integer).Value = produto.Estoque;
        comando.Parameters.Add("@Categoria", SqliteType.Text).Value = produto.Categoria.Trim();
    }

    /// <summary>
    /// Mapeamento manual de uma linha do DataReader para um objeto Produto.
    /// </summary>
    private static Produto Mapear(SqliteDataReader reader)
    {
        var ordinalDataExclusao = reader.GetOrdinal("DataExclusao");

        return new Produto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Nome = reader.GetString(reader.GetOrdinal("Nome")),
            Preco = Math.Round(reader.GetDecimal(reader.GetOrdinal("Preco")), 2),
            Estoque = reader.GetInt32(reader.GetOrdinal("Estoque")),
            Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
            Ativo = reader.GetBoolean(reader.GetOrdinal("Ativo")),
            DataCadastro = reader.GetDateTime(reader.GetOrdinal("DataCadastro")),
            DataExclusao = reader.IsDBNull(ordinalDataExclusao) ? null : reader.GetDateTime(ordinalDataExclusao)
        };
    }

    private static void ValidarId(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), id, "O Id deve ser um número maior que zero.");
    }

    private static void ValidarProduto(Produto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        var erros = produto.Validar();
        if (erros.Count > 0)
            throw new ArgumentException(string.Join(Environment.NewLine, erros), nameof(produto));
    }

    /// <summary>
    /// Centraliza o tratamento de exceções do banco: registra o erro técnico no log
    /// e relança como <see cref="RepositorioException"/> com uma mensagem amigável.
    /// </summary>
    private T Executar<T>(string operacao, Func<T> acao)
    {
        try
        {
            return acao();
        }
        catch (SqliteException ex)
        {
            var mensagem = TraduzirErro(ex);
            _logger.Erro(operacao, $"{mensagem} [SQLite {ex.SqliteErrorCode}/{ex.SqliteExtendedErrorCode}] {ex.Message}");
            throw new RepositorioException(operacao, mensagem, ex);
        }
        catch (InvalidOperationException ex)
        {
            // Ex.: connection string inválida ou conexão em estado incorreto.
            const string mensagem = "Não foi possível executar a operação no banco de dados. Verifique a connection string.";
            _logger.Erro(operacao, $"{mensagem} {ex.Message}");
            throw new RepositorioException(operacao, mensagem, ex);
        }
    }

    internal static string TraduzirErro(SqliteException ex) => ex.SqliteErrorCode switch
    {
        1 => $"Erro no comando SQL: {ex.Message}",
        5 or 6 => "O banco de dados está em uso por outro processo. Tente novamente em instantes.",
        8 => "O banco de dados está somente leitura.",
        11 or 26 => "O arquivo do banco está corrompido ou não é um banco SQLite.",
        13 => "Não há espaço em disco para gravar no banco de dados.",
        14 => "Não foi possível abrir o arquivo do banco. Verifique a connection string no appsettings.json.",
        19 => "Os dados violam uma regra da tabela (campo obrigatório, preço ou estoque negativo, tamanho máximo).",
        _ => $"Erro inesperado no banco de dados (código {ex.SqliteErrorCode})."
    };
}
