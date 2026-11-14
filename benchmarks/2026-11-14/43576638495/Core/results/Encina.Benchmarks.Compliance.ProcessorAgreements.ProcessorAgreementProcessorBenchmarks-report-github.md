```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.55GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;RegisterProcessor (new processor)&#39;        | 2.072 μs | 0.5720 μs | 0.0314 μs |  1.00 |    0.02 |    2 | 0.0191 | 0.0153 |     568 B |        1.00 |
| &#39;GetProcessor by ID (cached read)&#39;         | 1.190 μs | 0.3118 μs | 0.0171 μs |  0.57 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
| &#39;GetFullSubProcessorChain (BFS traversal)&#39; | 1.151 μs | 0.2148 μs | 0.0118 μs |  0.56 |    0.01 |    1 | 0.0153 | 0.0134 |     424 B |        0.75 |
