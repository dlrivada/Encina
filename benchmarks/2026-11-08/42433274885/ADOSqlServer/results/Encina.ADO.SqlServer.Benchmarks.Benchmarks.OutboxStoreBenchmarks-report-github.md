```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                           | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|--------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 521.9 μs | 4.92 μs | 6.57 μs |  1.00 |    0.02 | 0.9766 |      - |  17.63 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 595.9 μs | 5.18 μs | 7.26 μs |  1.14 |    0.02 | 6.8359 | 0.9766 | 125.61 KB |        7.12 |
