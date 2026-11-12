```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   298.0 μs |   515.1 μs | 28.24 μs |  1.00 |    0.11 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 3,159.5 μs | 1,074.2 μs | 58.88 μs | 10.57 |    0.74 | 2197.47 KB |       68.79 |
| AddAsync                |   190.0 μs |   340.5 μs | 18.66 μs |  0.64 |    0.07 |   23.56 KB |        0.74 |
| GetMessageAsync         |   300.1 μs |   423.1 μs | 23.19 μs |  1.00 |    0.10 |   31.95 KB |        1.00 |
