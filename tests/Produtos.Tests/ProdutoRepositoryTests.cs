using Microsoft.Data.Sqlite;
using Produtos.Core.Data;
using Produtos.Core.Models;

namespace Produtos.Tests;

[TestClass]
public sealed class ProdutoRepositoryTests
{
    private BancoDeTeste _banco = null!;
    private ProdutoRepository Repositorio => _banco.Repositorio;

    [TestInitialize]
    public void Inicializar() => _banco = new BancoDeTeste();

    [TestCleanup]
    public void Limpar() => _banco.Dispose();

    private static Produto NovoProduto(string nome = "Teclado mecânico", decimal preco = 249.90m,
        int estoque = 10, string categoria = "Informática") =>
        new() { Nome = nome, Preco = preco, Estoque = estoque, Categoria = categoria };

    // ------------------------------------------------------------------ CRUD

    [TestMethod]
    public void Inserir_DevolveIdGerado_EPersisteOsDados()
    {
        var id = Repositorio.Inserir(NovoProduto());

        var salvo = Repositorio.BuscarPorId(id);

        Assert.IsGreaterThan(0, id);
        Assert.IsNotNull(salvo);
        Assert.AreEqual("Teclado mecânico", salvo.Nome);
        Assert.AreEqual(249.90m, salvo.Preco);
        Assert.AreEqual(10, salvo.Estoque);
        Assert.AreEqual("Informática", salvo.Categoria);
        Assert.IsTrue(salvo.Ativo);
    }

    [TestMethod]
    public void Listar_RetornaTodosOsProdutosAtivosEmOrdemDeId()
    {
        var id1 = Repositorio.Inserir(NovoProduto("Mouse"));
        var id2 = Repositorio.Inserir(NovoProduto("Monitor"));

        var lista = Repositorio.Listar();

        CollectionAssert.AreEqual(new[] { id1, id2 }, lista.Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public void BuscarPorId_Inexistente_RetornaNull()
    {
        Assert.IsNull(Repositorio.BuscarPorId(999));
    }

    [TestMethod]
    public void Atualizar_AlteraTodosOsCampos()
    {
        var id = Repositorio.Inserir(NovoProduto());

        var alterado = NovoProduto("Teclado sem fio", 199.00m, 3, "Eletrônicos");
        alterado.Id = id;
        var ok = Repositorio.Atualizar(alterado);

        var salvo = Repositorio.BuscarPorId(id)!;
        Assert.IsTrue(ok);
        Assert.AreEqual("Teclado sem fio", salvo.Nome);
        Assert.AreEqual(199.00m, salvo.Preco);
        Assert.AreEqual(3, salvo.Estoque);
        Assert.AreEqual("Eletrônicos", salvo.Categoria);
    }

    [TestMethod]
    public void Atualizar_IdInexistente_RetornaFalse()
    {
        var produto = NovoProduto();
        produto.Id = 42;

        Assert.IsFalse(Repositorio.Atualizar(produto));
    }

    // ------------------------------------------------------------------ Soft delete

    [TestMethod]
    public void Excluir_FazSoftDelete_ProdutoSomeDaListaMasContinuaNoBanco()
    {
        var id = Repositorio.Inserir(NovoProduto());

        Assert.IsTrue(Repositorio.Excluir(id));

        Assert.IsEmpty(Repositorio.Listar());
        Assert.IsNull(Repositorio.BuscarPorId(id));

        var excluido = Repositorio.BuscarPorId(id, incluirExcluidos: true);
        Assert.IsNotNull(excluido);
        Assert.IsFalse(excluido.Ativo);
        Assert.IsNotNull(excluido.DataExclusao);
    }

    [TestMethod]
    public void Excluir_DuasVezes_SegundaRetornaFalse()
    {
        var id = Repositorio.Inserir(NovoProduto());

        Assert.IsTrue(Repositorio.Excluir(id));
        Assert.IsFalse(Repositorio.Excluir(id));
    }

    [TestMethod]
    public void Atualizar_ProdutoExcluido_NaoAltera()
    {
        var id = Repositorio.Inserir(NovoProduto());
        Repositorio.Excluir(id);

        var alterado = NovoProduto("Outro nome");
        alterado.Id = id;

        Assert.IsFalse(Repositorio.Atualizar(alterado));
        Assert.AreEqual("Teclado mecânico", Repositorio.BuscarPorId(id, incluirExcluidos: true)!.Nome);
    }

    [TestMethod]
    public void Restaurar_DesfazOSoftDelete()
    {
        var id = Repositorio.Inserir(NovoProduto());
        Repositorio.Excluir(id);

        Assert.IsTrue(Repositorio.Restaurar(id));

        var restaurado = Repositorio.BuscarPorId(id);
        Assert.IsNotNull(restaurado);
        Assert.IsTrue(restaurado.Ativo);
        Assert.IsNull(restaurado.DataExclusao);
    }

    [TestMethod]
    public void ExcluirPermanentemente_RemoveALinha()
    {
        var id = Repositorio.Inserir(NovoProduto());

        Assert.IsTrue(Repositorio.ExcluirPermanentemente(id));
        Assert.IsNull(Repositorio.BuscarPorId(id, incluirExcluidos: true));
    }

    // ------------------------------------------------------------------ SQL Injection

    [TestMethod]
    [DataRow("'; DROP TABLE Produtos; --")]
    [DataRow("x' OR '1'='1")]
    [DataRow("Robert'); DELETE FROM Produtos; --")]
    public void Inserir_TextoMalicioso_EGravadoLiteralmente_ETabelaContinuaIntacta(string nomeMalicioso)
    {
        var idLegitimo = Repositorio.Inserir(NovoProduto("Produto legítimo"));

        var idMalicioso = Repositorio.Inserir(NovoProduto(nomeMalicioso, categoria: nomeMalicioso));

        var lista = Repositorio.Listar();
        Assert.HasCount(2, lista);
        Assert.AreEqual("Produto legítimo", lista.Single(p => p.Id == idLegitimo).Nome);
        Assert.AreEqual(nomeMalicioso, lista.Single(p => p.Id == idMalicioso).Nome);
    }

    [TestMethod]
    public void Atualizar_TextoMalicioso_AlteraSomenteOProdutoInformado()
    {
        var idA = Repositorio.Inserir(NovoProduto("A"));
        var idB = Repositorio.Inserir(NovoProduto("B"));

        var ataque = NovoProduto("Hackeado' WHERE 1=1; --");
        ataque.Id = idA;
        Repositorio.Atualizar(ataque);

        Assert.AreEqual("Hackeado' WHERE 1=1; --", Repositorio.BuscarPorId(idA)!.Nome);
        Assert.AreEqual("B", Repositorio.BuscarPorId(idB)!.Nome);
    }

    // ------------------------------------------------------------------ Validação e erros

    [TestMethod]
    [DataRow("", 10.0, 1, "Informática")]
    [DataRow("Mouse", -1.0, 1, "Informática")]
    [DataRow("Mouse", 10.0, -5, "Informática")]
    [DataRow("Mouse", 10.0, 1, "  ")]
    public void Inserir_ProdutoInvalido_LancaArgumentException_ENaoGrava(string nome, double preco, int estoque, string categoria)
    {
        Assert.ThrowsExactly<ArgumentException>(() => Repositorio.Inserir(NovoProduto(nome, (decimal)preco, estoque, categoria)));
        Assert.IsEmpty(Repositorio.Listar());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void Operacoes_ComIdInvalido_LancamArgumentOutOfRange(int id)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Repositorio.BuscarPorId(id));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Repositorio.Excluir(id));
    }

    [TestMethod]
    public void ErroDoBanco_ViraRepositorioException_ComExcecaoOriginal()
    {
        // Banco sem a tabela Produtos: o SELECT falha com "no such table".
        using var bancoSemTabela = new BancoDeTeste(criarTabela: false);

        var ex = Assert.ThrowsExactly<RepositorioException>(() => bancoSemTabela.Repositorio.Listar());

        Assert.AreEqual("LISTAR", ex.Operacao);
        Assert.IsInstanceOfType<SqliteException>(ex.InnerException);
        StringAssert.Contains(ex.Message, "no such table");
        StringAssert.Contains(string.Join('\n', bancoSemTabela.LinhasDoLog()), "ERRO  | LISTAR");
    }

    [TestMethod]
    public void ViolacaoDeRegraDaTabela_EBarradaPeloBanco()
    {
        // Mesmo gravando direto no banco (sem passar pela validação do repositório),
        // os CHECKs do script impedem estoque negativo.
        using var conexao = new SqliteConnection(_banco.ConnectionString);
        conexao.Open();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "INSERT INTO Produtos (Nome, Preco, Estoque, Categoria) VALUES (@Nome, @Preco, @Estoque, @Categoria);";
        comando.Parameters.AddWithValue("@Nome", "Teste");
        comando.Parameters.AddWithValue("@Preco", 1.0);
        comando.Parameters.AddWithValue("@Estoque", -1);
        comando.Parameters.AddWithValue("@Categoria", "X");

        var ex = Assert.ThrowsExactly<SqliteException>(() => comando.ExecuteNonQuery());
        Assert.AreEqual(19, ex.SqliteErrorCode); // SQLITE_CONSTRAINT
    }

    // ------------------------------------------------------------------ Log

    [TestMethod]
    public void OperacoesSaoRegistradasNoArquivoDeLog()
    {
        var id = Repositorio.Inserir(NovoProduto());
        Repositorio.Listar();
        Repositorio.BuscarPorId(id);
        Repositorio.Excluir(id);

        var log = string.Join('\n', _banco.LinhasDoLog());

        StringAssert.Contains(log, "INICIALIZAR");
        StringAssert.Contains(log, $"INSERIR      | Produto Id={id}");
        StringAssert.Contains(log, "LISTAR");
        StringAssert.Contains(log, "BUSCAR");
        StringAssert.Contains(log, $"Produto Id={id} marcado como excluído");
    }
}
