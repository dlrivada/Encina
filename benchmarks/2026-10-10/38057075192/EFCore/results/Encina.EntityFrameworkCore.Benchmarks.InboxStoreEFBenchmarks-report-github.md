```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   237.9 μs | 501.4 μs | 27.48 μs |  1.21 |    0.16 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 4,511.8 μs | 541.3 μs | 29.67 μs | 22.97 |    1.96 | 2189.73 KB |       68.55 |
| AddAsync                |   128.6 μs | 262.3 μs | 14.38 μs |  0.65 |    0.08 |   23.56 KB |        0.74 |
| GetMessageAsync         |   197.7 μs | 368.7 μs | 20.21 μs |  1.01 |    0.12 |   31.95 KB |        1.00 |
