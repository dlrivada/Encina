```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   194.1 μs |   275.2 μs | 15.08 μs |  1.18 |    0.11 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 2,349.4 μs | 1,086.6 μs | 59.56 μs | 14.31 |    0.99 | 2189.73 KB |       68.55 |
| AddAsync                |   108.7 μs |   141.9 μs |  7.78 μs |  0.66 |    0.06 |   23.56 KB |        0.74 |
| GetMessageAsync         |   164.8 μs |   224.3 μs | 12.29 μs |  1.00 |    0.09 |   31.95 KB |        1.00 |
