// Cada teste usa seu próprio arquivo de banco, então podem rodar em paralelo.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
