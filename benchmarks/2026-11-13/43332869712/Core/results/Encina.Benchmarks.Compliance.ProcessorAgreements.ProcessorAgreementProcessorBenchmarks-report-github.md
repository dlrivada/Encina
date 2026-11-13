```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.007 μs | 0.2506 μs | 0.0137 μs |  1.00 |    2 | 0.0305 | 0.0267 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.089 μs | 0.6042 μs | 0.0331 μs |  0.54 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.022 μs | 0.2780 μs | 0.0152 μs |  0.51 |    1 | 0.0248 | 0.0229 |     424 B |        0.75 |
