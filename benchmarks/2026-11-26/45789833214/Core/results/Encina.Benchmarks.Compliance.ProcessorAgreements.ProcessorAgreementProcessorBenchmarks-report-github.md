```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 1,840.9 ns | 508.2 ns | 27.86 ns |  1.00 |    0.02 |    2 | 0.0057 | 0.0038 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         |   893.9 ns | 518.4 ns | 28.41 ns |  0.49 |    0.01 |    1 | 0.0038 | 0.0019 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; |   942.0 ns | 748.3 ns | 41.01 ns |  0.51 |    0.02 |    1 | 0.0038 | 0.0019 |     424 B |        0.75 |
