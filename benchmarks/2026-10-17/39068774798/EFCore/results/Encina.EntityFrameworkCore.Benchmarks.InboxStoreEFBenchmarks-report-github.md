```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   281.1 μs | 199.2 μs | 10.92 μs |  1.04 |    0.07 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 3,001.0 μs | 324.6 μs | 17.79 μs | 11.12 |    0.62 | 2189.73 KB |       68.55 |
| AddAsync                |   173.4 μs | 253.3 μs | 13.89 μs |  0.64 |    0.06 |   23.56 KB |        0.74 |
| GetMessageAsync         |   270.6 μs | 323.3 μs | 17.72 μs |  1.00 |    0.08 |   31.95 KB |        1.00 |
