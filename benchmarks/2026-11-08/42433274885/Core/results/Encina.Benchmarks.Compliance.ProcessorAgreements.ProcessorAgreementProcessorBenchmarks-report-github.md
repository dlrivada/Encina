```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1.991 μs | 0.0280 μs | 0.0420 μs |  1.00 |    0.03 |    2 | 0.0305 | 0.0267 |     632 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.072 μs | 0.0192 μs | 0.0287 μs |  0.54 |    0.02 |    1 | 0.0248 | 0.0229 |     456 B |        0.72 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.081 μs | 0.0162 μs | 0.0242 μs |  0.54 |    0.02 |    1 | 0.0248 | 0.0229 |     456 B |        0.72 |
