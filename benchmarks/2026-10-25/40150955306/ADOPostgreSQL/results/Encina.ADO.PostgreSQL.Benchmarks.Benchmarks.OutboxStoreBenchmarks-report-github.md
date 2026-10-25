```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                           | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|--------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 124.9 μs | 1.79 μs |  2.68 μs |  1.00 |    0.03 | 0.2441 |      - |   5.34 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 253.2 μs | 8.00 μs | 11.97 μs |  2.03 |    0.10 | 2.4414 | 0.2441 |  40.25 KB |        7.53 |
