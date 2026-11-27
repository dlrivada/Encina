```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 117.2 μs |   2.45 μs |  0.13 μs |  1.00 |    0.00 | 0.2441 |      - |   5.34 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 247.5 μs | 195.43 μs | 10.71 μs |  2.11 |    0.08 | 2.4414 | 0.2441 |  40.25 KB |        7.53 |
