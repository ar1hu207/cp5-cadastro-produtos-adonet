# Roteiro do vídeo (3 a 5 min)

Deixe antes de gravar: a aplicação fechada, o banco zerado (apague `produtos.db` na pasta
`bin/Debug/net10.0-windows`) e o Visual Studio ou VS Code aberto no projeto.

| Tempo | O que mostrar | O que falar |
|---|---|---|
| 0:00 – 0:30 | README / estrutura de pastas | Tema, tecnologias (WPF, ADO.NET, SQLite) e divisão em `Produtos.Core` (dados) e `Produtos.Wpf` (tela). |
| 0:30 – 1:00 | `appsettings.json` e `criar_tabela_produtos.sql` | A connection string fica no appsettings; o script cria a tabela e é executado na inicialização. |
| 1:00 – 2:30 | Aplicação rodando | **Inserir** 2 ou 3 produtos → **Listar** → **Buscar por ID** → **Atualizar** o preço de um → **Excluir** um (soft delete) → marcar "Mostrar excluídos" e **Restaurar**. Tentar salvar com preço negativo para mostrar a validação. |
| 2:30 – 3:30 | `ProdutoRepository.cs` | Mostrar um `INSERT` com `Parameters.Add`, o `ExecuteNonQuery`, o `ExecuteReader` do `Listar` e o método `Mapear`. Explicar que nenhum valor digitado entra concatenado no SQL. Citar o teste com `'; DROP TABLE Produtos; --`. |
| 3:30 – 4:15 | Método `Executar` + `logs/operacoes.log` | `SqliteException` vira `RepositorioException` com mensagem amigável e o erro técnico vai para o log. Abrir o arquivo de log e mostrar as operações que acabaram de ser feitas. |
| 4:15 – 5:00 | Terminal: `dotnet test` | Bônus: transações, soft delete e 23 testes passando. Encerrar com **Sair**. |
