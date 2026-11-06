```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   302.9 μs | 342.9 μs | 18.79 μs |  0.98 |    0.07 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 3,461.4 μs | 599.8 μs | 32.87 μs | 11.18 |    0.54 | 2197.47 KB |       68.79 |
| AddAsync                |   184.5 μs | 275.0 μs | 15.07 μs |  0.60 |    0.05 |   23.56 KB |        0.74 |
| GetMessageAsync         |   310.4 μs | 319.8 μs | 17.53 μs |  1.00 |    0.07 |   31.95 KB |        1.00 |
