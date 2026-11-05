```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.018 μs | 0.5677 μs | 0.0311 μs |  1.00 |    0.02 |    2 | 0.0305 | 0.0267 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.075 μs | 0.2897 μs | 0.0159 μs |  0.53 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.012 μs | 0.4897 μs | 0.0268 μs |  0.50 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
