using Produtos.Core.Models;

namespace Produtos.Core.Data;

public interface IProdutoRepository
{
    /// <summary>Insere o produto e devolve o Id gerado pelo banco.</summary>
    int Inserir(Produto produto);

    List<Produto> Listar(bool incluirExcluidos = false);

    /// <summary>Retorna null quando o Id não existe.</summary>
    Produto? BuscarPorId(int id, bool incluirExcluidos = false);

    /// <summary>Retorna false quando o produto não existe ou está excluído.</summary>
    bool Atualizar(Produto produto);

    /// <summary>Soft delete: marca o produto como excluído sem apagar a linha.</summary>
    bool Excluir(int id);

    /// <summary>Desfaz o soft delete.</summary>
    bool Restaurar(int id);

    /// <summary>DELETE físico: remove a linha da tabela.</summary>
    bool ExcluirPermanentemente(int id);
}
