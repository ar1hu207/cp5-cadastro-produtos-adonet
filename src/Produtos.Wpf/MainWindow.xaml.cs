using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Produtos.Core.Data;
using Produtos.Core.Models;

namespace Produtos.Wpf;

/// <summary>
/// Camada de interface: lê os campos, chama o <see cref="IProdutoRepository"/> e mostra o resultado.
/// Não há nenhum SQL aqui.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IProdutoRepository _repositorio;
    private Produto? _produtoEmEdicao;
    private Produto? _produtoParaExcluir;

    public MainWindow(IProdutoRepository repositorio, string caminhoBanco, string caminhoLog)
    {
        InitializeComponent();
        _repositorio = repositorio;

        txtInfoBanco.Text = $"Banco: {caminhoBanco}   ·   Log: {caminhoLog}";
        txtInfoBanco.ToolTip = txtInfoBanco.Text;

        navInserir.IsChecked = true;
    }

    // ================================================================== Menu

    private void Menu_Checked(object sender, RoutedEventArgs e)
    {
        pnlInserir.Visibility = sender == navInserir ? Visibility.Visible : Visibility.Collapsed;
        pnlListar.Visibility = sender == navListar ? Visibility.Visible : Visibility.Collapsed;
        pnlBuscar.Visibility = sender == navBuscar ? Visibility.Visible : Visibility.Collapsed;
        pnlAtualizar.Visibility = sender == navAtualizar ? Visibility.Visible : Visibility.Collapsed;
        pnlExcluir.Visibility = sender == navExcluir ? Visibility.Visible : Visibility.Collapsed;

        if (sender == navInserir) formInserir.Focar();
        else if (sender == navListar) CarregarLista();
        else if (sender == navBuscar) txtIdBuscar.Focus();
        else if (sender == navAtualizar) txtIdAtualizar.Focus();
        else if (sender == navExcluir) txtIdExcluir.Focus();
    }

    private void BtnSair_Click(object sender, RoutedEventArgs e)
    {
        var resposta = MessageBox.Show("Deseja realmente sair?", "Sair",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (resposta == MessageBoxResult.Yes)
            Close();
    }

    // ================================================================== 1. Inserir

    private void BtnSalvarNovo_Click(object sender, RoutedEventArgs e)
    {
        var produto = formInserir.Ler(out var erros);
        if (produto is null)
        {
            MostrarAviso(erros);
            return;
        }

        ExecutarOperacao(() =>
        {
            var id = _repositorio.Inserir(produto);
            formInserir.Limpar();
            formInserir.Focar();
            MostrarSucesso($"Produto #{id} \"{produto.Nome}\" inserido com sucesso.");
        });
    }

    private void BtnLimparNovo_Click(object sender, RoutedEventArgs e)
    {
        formInserir.Limpar();
        formInserir.Focar();
    }

    // ================================================================== 2. Listar

    private void CarregarLista()
    {
        ExecutarOperacao(() =>
        {
            var produtos = _repositorio.Listar(incluirExcluidos: chkMostrarExcluidos.IsChecked == true);
            gridProdutos.ItemsSource = produtos;

            var ativos = produtos.Where(p => p.Ativo).ToList();
            var valorEmEstoque = ativos.Sum(p => p.Preco * p.Estoque);
            txtResumoLista.Text =
                $"{produtos.Count} produto(s) na lista  ·  {ativos.Count} ativo(s)  ·  " +
                $"valor em estoque: {valorEmEstoque.ToString("C", CultureInfo.CurrentCulture)}";

            MostrarStatus($"{produtos.Count} produto(s) carregado(s).", Brushes.Black);
        });
    }

    private void BtnRecarregar_Click(object sender, RoutedEventArgs e) => CarregarLista();

    private void ChkMostrarExcluidos_Changed(object sender, RoutedEventArgs e) => CarregarLista();

    private void GridProdutos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        btnRestaurar.IsEnabled = gridProdutos.SelectedItem is Produto { Ativo: false };
    }

    private void GridProdutos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (gridProdutos.SelectedItem is not Produto { Ativo: true } produto)
            return;

        txtIdAtualizar.Text = produto.Id.ToString();
        navAtualizar.IsChecked = true;
        CarregarParaAtualizar();
    }

    private void BtnRestaurar_Click(object sender, RoutedEventArgs e)
    {
        if (gridProdutos.SelectedItem is not Produto produto)
            return;

        ExecutarOperacao(() =>
        {
            if (_repositorio.Restaurar(produto.Id))
            {
                CarregarLista();
                MostrarSucesso($"Produto #{produto.Id} \"{produto.Nome}\" restaurado.");
            }
            else
            {
                MostrarAviso($"O produto #{produto.Id} não está entre os excluídos.");
            }
        });
    }

    // ================================================================== 3. Buscar por ID

    private void BtnBuscar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void TxtIdBuscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Buscar();
    }

    private void Buscar()
    {
        cartaoBusca.Visibility = Visibility.Collapsed;
        if (!TentarLerId(txtIdBuscar, out var id))
            return;

        ExecutarOperacao(() =>
        {
            var produto = _repositorio.BuscarPorId(id, incluirExcluidos: true);
            if (produto is null)
            {
                MostrarAviso($"Nenhum produto encontrado com o ID {id}.");
                return;
            }

            conteudoBusca.Content = produto;
            cartaoBusca.Visibility = Visibility.Visible;
            MostrarSucesso($"Produto #{produto.Id} encontrado.");
        });
    }

    // ================================================================== 4. Atualizar

    private void BtnCarregarAtualizar_Click(object sender, RoutedEventArgs e) => CarregarParaAtualizar();

    private void TxtIdAtualizar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CarregarParaAtualizar();
    }

    private void CarregarParaAtualizar()
    {
        DefinirProdutoEmEdicao(null);
        if (!TentarLerId(txtIdAtualizar, out var id))
            return;

        ExecutarOperacao(() =>
        {
            var produto = _repositorio.BuscarPorId(id);
            if (produto is null)
            {
                MostrarAviso($"Nenhum produto ativo encontrado com o ID {id}.");
                return;
            }

            DefinirProdutoEmEdicao(produto);
            MostrarStatus($"Editando o produto #{produto.Id}.", Brushes.Black);
        });
    }

    private void BtnSalvarAtualizacao_Click(object sender, RoutedEventArgs e)
    {
        if (_produtoEmEdicao is null)
            return;

        var produto = formAtualizar.Ler(out var erros);
        if (produto is null)
        {
            MostrarAviso(erros);
            return;
        }

        produto.Id = _produtoEmEdicao.Id;

        ExecutarOperacao(() =>
        {
            if (_repositorio.Atualizar(produto))
            {
                DefinirProdutoEmEdicao(produto);
                MostrarSucesso($"Produto #{produto.Id} atualizado com sucesso.");
            }
            else
            {
                DefinirProdutoEmEdicao(null);
                MostrarAviso($"O produto #{produto.Id} não existe mais ou foi excluído. Nada foi alterado.");
            }
        });
    }

    private void DefinirProdutoEmEdicao(Produto? produto)
    {
        _produtoEmEdicao = produto;
        formAtualizar.IsEnabled = produto is not null;
        btnSalvarAtualizacao.IsEnabled = produto is not null;

        if (produto is null)
        {
            formAtualizar.Limpar();
            txtEditando.Text = "Nenhum produto carregado.";
        }
        else
        {
            formAtualizar.Preencher(produto);
            txtEditando.Text = $"Editando o produto #{produto.Id}";
        }
    }

    // ================================================================== 5. Excluir

    private void BtnCarregarExcluir_Click(object sender, RoutedEventArgs e) => CarregarParaExcluir();

    private void TxtIdExcluir_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CarregarParaExcluir();
    }

    private void CarregarParaExcluir()
    {
        _produtoParaExcluir = null;
        cartaoExcluir.Visibility = Visibility.Collapsed;
        if (!TentarLerId(txtIdExcluir, out var id))
            return;

        ExecutarOperacao(() =>
        {
            var produto = _repositorio.BuscarPorId(id);
            if (produto is null)
            {
                MostrarAviso($"Nenhum produto ativo encontrado com o ID {id}.");
                return;
            }

            _produtoParaExcluir = produto;
            conteudoExcluir.Content = produto;
            chkExcluirPermanente.IsChecked = false;
            cartaoExcluir.Visibility = Visibility.Visible;
            MostrarStatus($"Confirme a exclusão do produto #{produto.Id}.", Brushes.Black);
        });
    }

    private void BtnExcluir_Click(object sender, RoutedEventArgs e)
    {
        if (_produtoParaExcluir is null)
            return;

        var produto = _produtoParaExcluir;
        var permanente = chkExcluirPermanente.IsChecked == true;
        var pergunta = permanente
            ? $"Apagar DEFINITIVAMENTE o produto #{produto.Id} \"{produto.Nome}\"?\n\nEsta ação não pode ser desfeita."
            : $"Excluir o produto #{produto.Id} \"{produto.Nome}\"?\n\nEle poderá ser restaurado na tela Listar.";

        var resposta = MessageBox.Show(pergunta, "Confirmar exclusão",
            MessageBoxButton.YesNo, permanente ? MessageBoxImage.Warning : MessageBoxImage.Question);
        if (resposta != MessageBoxResult.Yes)
            return;

        ExecutarOperacao(() =>
        {
            var excluido = permanente
                ? _repositorio.ExcluirPermanentemente(produto.Id)
                : _repositorio.Excluir(produto.Id);

            _produtoParaExcluir = null;
            cartaoExcluir.Visibility = Visibility.Collapsed;
            txtIdExcluir.Clear();

            if (excluido)
                MostrarSucesso(permanente
                    ? $"Produto #{produto.Id} apagado definitivamente."
                    : $"Produto #{produto.Id} excluído (soft delete).");
            else
                MostrarAviso($"O produto #{produto.Id} não existe mais ou já estava excluído.");
        });
    }

    // ================================================================== Apoio

    /// <summary>
    /// Executa uma chamada ao repositório tratando os erros de banco e de validação.
    /// </summary>
    private void ExecutarOperacao(Action acao)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            acao();
        }
        catch (RepositorioException ex)
        {
            MostrarStatus($"Erro de banco de dados: {ex.Message}", Brushes.Firebrick);
            MessageBox.Show(ex.Message, "Erro de banco de dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (ArgumentException ex)
        {
            MostrarAviso(ex.Message);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private bool TentarLerId(TextBox campo, out int id)
    {
        if (int.TryParse(campo.Text.Trim(), out id) && id > 0)
            return true;

        MostrarAviso("Informe um ID numérico maior que zero.");
        campo.Focus();
        campo.SelectAll();
        return false;
    }

    private void MostrarSucesso(string mensagem) =>
        MostrarStatus(mensagem, (Brush)FindResource("CorSucesso"));

    private void MostrarAviso(IEnumerable<string> mensagens) =>
        MostrarAviso(string.Join(Environment.NewLine, mensagens));

    private void MostrarAviso(string mensagem)
    {
        MostrarStatus(mensagem.Split(Environment.NewLine)[0], (Brush)FindResource("CorAviso"));
        MessageBox.Show(mensagem, "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void MostrarStatus(string mensagem, Brush cor)
    {
        txtStatus.Text = mensagem;
        txtStatus.Foreground = cor;
    }
}
