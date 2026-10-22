```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.134 μs | 0.0287 μs | 0.0421 μs |  1.00 |    0.03 |    2 | 0.0305 | 0.0267 |     632 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.175 μs | 0.0251 μs | 0.0369 μs |  0.55 |    0.02 |    1 | 0.0248 | 0.0229 |     456 B |        0.72 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.124 μs | 0.0118 μs | 0.0177 μs |  0.53 |    0.01 |    1 | 0.0248 | 0.0229 |     456 B |        0.72 |
