```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   293.9 μs | 115.4 μs |  6.32 μs |  0.99 |    0.03 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 3,253.9 μs | 545.6 μs | 29.91 μs | 10.94 |    0.32 | 2197.78 KB |       68.80 |
| AddAsync                |   181.9 μs | 203.1 μs | 11.13 μs |  0.61 |    0.04 |   23.56 KB |        0.74 |
| GetMessageAsync         |   297.6 μs | 177.5 μs |  9.73 μs |  1.00 |    0.04 |   31.95 KB |        1.00 |
