```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.020 μs | 0.2929 μs | 0.0161 μs |  1.00 |    2 | 0.0191 | 0.0153 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.193 μs | 0.2350 μs | 0.0129 μs |  0.59 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.121 μs | 0.2095 μs | 0.0115 μs |  0.56 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
