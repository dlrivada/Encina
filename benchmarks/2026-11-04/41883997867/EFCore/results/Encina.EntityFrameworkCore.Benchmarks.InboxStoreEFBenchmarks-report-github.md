```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|------------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   214.2 μs |   121.87 μs |  6.68 μs |  1.10 |    0.03 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 4,485.0 μs | 1,146.94 μs | 62.87 μs | 23.00 |    0.44 | 2197.47 KB |       68.79 |
| AddAsync                |   128.8 μs |   155.64 μs |  8.53 μs |  0.66 |    0.04 |   23.56 KB |        0.74 |
| GetMessageAsync         |   195.1 μs |    61.68 μs |  3.38 μs |  1.00 |    0.02 |   31.95 KB |        1.00 |
