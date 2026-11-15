```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   214.9 μs |  60.85 μs |  3.34 μs |  1.10 |    0.04 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 4,587.4 μs | 342.20 μs | 18.76 μs | 23.51 |    0.79 | 2197.47 KB |       68.79 |
| AddAsync                |   136.9 μs | 143.12 μs |  7.84 μs |  0.70 |    0.04 |   23.56 KB |        0.74 |
| GetMessageAsync         |   195.3 μs | 136.09 μs |  7.46 μs |  1.00 |    0.05 |   31.95 KB |        1.00 |
