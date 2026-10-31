```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   216.4 μs |   483.6 μs | 26.51 μs |  0.79 |    0.13 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 2,619.4 μs | 1,642.3 μs | 90.02 μs |  9.52 |    1.29 | 2189.73 KB |       68.55 |
| AddAsync                |   138.7 μs |   305.1 μs | 16.72 μs |  0.50 |    0.09 |   23.56 KB |        0.74 |
| GetMessageAsync         |   279.4 μs |   752.6 μs | 41.25 μs |  1.02 |    0.19 |   31.95 KB |        1.00 |
