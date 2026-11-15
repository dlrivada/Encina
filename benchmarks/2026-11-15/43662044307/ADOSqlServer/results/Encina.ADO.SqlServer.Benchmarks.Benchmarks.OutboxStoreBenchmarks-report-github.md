```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 557.4 μs | 17.61 μs | 25.81 μs |  1.00 |    0.06 | 0.9766 |      - |  17.63 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 630.4 μs |  2.54 μs |  3.65 μs |  1.13 |    0.05 | 6.8359 | 0.9766 | 125.61 KB |        7.13 |
