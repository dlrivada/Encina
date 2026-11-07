```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1.936 μs | 1.1939 μs | 0.0654 μs |  1.00 |    0.04 |    2 | 0.0305 | 0.0267 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.054 μs | 0.3869 μs | 0.0212 μs |  0.55 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.042 μs | 0.7055 μs | 0.0387 μs |  0.54 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
