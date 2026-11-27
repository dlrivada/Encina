```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   225.9 μs | 1,437.5 μs | 78.80 μs |  1.21 |    0.37 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 2,388.5 μs | 1,168.5 μs | 64.05 μs | 12.77 |    0.73 | 2197.47 KB |       68.79 |
| AddAsync                |   101.5 μs |   161.8 μs |  8.87 μs |  0.54 |    0.05 |   23.56 KB |        0.74 |
| GetMessageAsync         |   187.5 μs |   202.5 μs | 11.10 μs |  1.00 |    0.07 |   31.95 KB |        1.00 |
