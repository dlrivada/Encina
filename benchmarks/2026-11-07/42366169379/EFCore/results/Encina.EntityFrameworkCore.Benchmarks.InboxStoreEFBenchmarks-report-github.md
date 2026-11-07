```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   216.9 μs | 809.5 μs | 44.37 μs |  1.32 |    0.25 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 2,297.8 μs | 644.1 μs | 35.31 μs | 13.94 |    0.83 | 2197.77 KB |       68.80 |
| AddAsync                |   104.5 μs | 192.3 μs | 10.54 μs |  0.63 |    0.07 |   23.56 KB |        0.74 |
| GetMessageAsync         |   165.3 μs | 199.2 μs | 10.92 μs |  1.00 |    0.08 |   31.95 KB |        1.00 |
