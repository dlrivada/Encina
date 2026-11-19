```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   308.1 μs | 204.0 μs | 11.18 μs |  1.06 |    0.08 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 3,245.6 μs | 328.6 μs | 18.01 μs | 11.20 |    0.75 | 2197.47 KB |       68.79 |
| AddAsync                |   187.9 μs | 228.2 μs | 12.51 μs |  0.65 |    0.06 |   23.56 KB |        0.74 |
| GetMessageAsync         |   291.0 μs | 413.3 μs | 22.65 μs |  1.00 |    0.10 |   31.95 KB |        1.00 |
