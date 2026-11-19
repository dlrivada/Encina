```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.037 μs | 0.5079 μs | 0.0278 μs |  1.00 |    0.02 |    2 | 0.0191 | 0.0153 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.180 μs | 0.2595 μs | 0.0142 μs |  0.58 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.103 μs | 0.1297 μs | 0.0071 μs |  0.54 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
