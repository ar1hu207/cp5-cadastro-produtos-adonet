using System.Globalization;
using System.Windows.Controls;
using Produtos.Core.Models;

namespace Produtos.Wpf.Views;

/// <summary>
/// Formulário de produto reaproveitado nas telas Inserir e Atualizar.
/// Só converte o que foi digitado; quem grava é o repositório.
/// </summary>
public partial class ProdutoFormulario : UserControl
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private static readonly string[] CategoriasSugeridas =
    [
        "Alimentos", "Bebidas", "Eletrônicos", "Informática", "Limpeza", "Papelaria", "Vestuário"
    ];

    public ProdutoFormulario()
    {
        InitializeComponent();
        cmbCategoria.ItemsSource = CategoriasSugeridas;
    }

    public void Preencher(Produto produto)
    {
        txtNome.Text = produto.Nome;
        txtPreco.Text = produto.Preco.ToString("0.00", PtBr);
        txtEstoque.Text = produto.Estoque.ToString(PtBr);
        cmbCategoria.Text = produto.Categoria;
    }

    public void Limpar()
    {
        txtNome.Clear();
        txtPreco.Clear();
        txtEstoque.Clear();
        cmbCategoria.Text = string.Empty;
    }

    public void Focar() => txtNome.Focus();

    /// <summary>
    /// Converte os campos em um Produto. Retorna null e preenche <paramref name="erros"/>
    /// quando algo foi digitado de forma inválida.
    /// </summary>
    public Produto? Ler(out List<string> erros)
    {
        erros = [];

        if (!TentarConverterPreco(txtPreco.Text, out var preco))
            erros.Add("Preço inválido. Exemplo: 19,90");

        if (!int.TryParse(txtEstoque.Text.Trim(), NumberStyles.Integer, PtBr, out var estoque))
            erros.Add("O estoque deve ser um número inteiro. Exemplo: 25");

        var produto = new Produto
        {
            Nome = txtNome.Text.Trim(),
            Preco = preco,
            Estoque = estoque,
            Categoria = cmbCategoria.Text.Trim()
        };

        erros.AddRange(produto.Validar());
        return erros.Count == 0 ? produto : null;
    }

    /// <summary>Aceita "19,90", "1.234,56", "R$ 19,90" e também "19.90".</summary>
    private static bool TentarConverterPreco(string texto, out decimal preco)
    {
        texto = texto.Replace("R$", string.Empty).Trim();
        var cultura = texto.Contains(',') ? PtBr : CultureInfo.InvariantCulture;
        return decimal.TryParse(texto, NumberStyles.Number, cultura, out preco);
    }
}
