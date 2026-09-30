using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Produtos.Core.Data;
using Produtos.Core.Logging;
using Produtos.Wpf.Configuracao;

namespace Produtos.Wpf;

public partial class App : Application
{
    private IOperacaoLogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Datas e moeda no padrão brasileiro (R$ 19,90) em toda a interface.
        var ptBr = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = ptBr;
        CultureInfo.DefaultThreadCurrentUICulture = ptBr;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ptBr.IetfLanguageTag)));

        DispatcherUnhandledException += AoOcorrerErroNaoTratado;

        try
        {
            var config = AppConfig.Carregar();
            _logger = new ArquivoOperacaoLogger(config.CaminhoLog);
            _logger.Info("APP", "Aplicação iniciada.");

            DatabaseInitializer.Inicializar(config.ConnectionString, config.CaminhoScript, _logger);

            // A interface só conhece o repositório; nenhum SQL fica na tela.
            var repositorio = new ProdutoRepository(config.ConnectionString, _logger);

            var janela = new MainWindow(repositorio, config.CaminhoBanco, config.CaminhoLog);
            MainWindow = janela;
            janela.Show();
        }
        catch (Exception ex)
        {
            _logger?.Erro("APP", $"Falha na inicialização: {ex.Message}");
            MessageBox.Show(
                $"Não foi possível iniciar a aplicação.\n\n{ex.Message}",
                "Cadastro de Produtos", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("APP", "Aplicação encerrada.");
        base.OnExit(e);
    }

    /// <summary>
    /// Última rede de proteção: qualquer erro não previsto é registrado e mostrado,
    /// sem fechar a aplicação.
    /// </summary>
    private void AoOcorrerErroNaoTratado(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Erro("APP", $"Erro não tratado: {e.Exception}");
        MessageBox.Show(
            $"Ocorreu um erro inesperado.\n\n{e.Exception.Message}",
            "Cadastro de Produtos", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
