```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                           | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|--------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 533.2 μs | 4.23 μs | 6.20 μs |  1.00 |    0.02 | 0.9766 |      - |  17.63 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 635.8 μs | 2.81 μs | 4.21 μs |  1.19 |    0.02 | 6.8359 | 0.9766 |  125.6 KB |        7.12 |
