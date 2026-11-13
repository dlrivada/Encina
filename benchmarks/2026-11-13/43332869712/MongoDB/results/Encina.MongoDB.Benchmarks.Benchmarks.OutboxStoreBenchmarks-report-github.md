```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 208.5 μs | 51.97 μs | 2.85 μs |  1.00 |    0.02 | 1.2207 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 501.4 μs | 99.33 μs | 5.44 μs |  2.41 |    0.04 | 4.8828 |  93.64 KB |        4.50 |
