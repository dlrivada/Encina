```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1.661 μs | 0.8529 μs | 0.0467 μs |  1.00 |    0.03 |    2 | 0.0057 | 0.0038 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.024 μs | 0.4478 μs | 0.0245 μs |  0.62 |    0.02 |    1 | 0.0048 | 0.0038 |     440 B |        0.77 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.014 μs | 1.1912 μs | 0.0653 μs |  0.61 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        0.77 |
