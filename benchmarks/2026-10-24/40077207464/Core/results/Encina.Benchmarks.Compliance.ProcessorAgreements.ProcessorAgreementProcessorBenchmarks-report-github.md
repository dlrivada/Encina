```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,958.0 ns | 200.8 ns | 11.01 ns |  1.00 |    2 | 0.0305 | 0.0267 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1,020.0 ns | 323.4 ns | 17.73 ns |  0.52 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   974.3 ns | 432.9 ns | 23.73 ns |  0.50 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
