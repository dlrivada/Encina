```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   221.5 μs | 391.1 μs | 21.44 μs |  1.14 |    0.10 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 4,474.5 μs | 507.2 μs | 27.80 μs | 22.95 |    0.85 | 2189.73 KB |       68.55 |
| AddAsync                |   146.0 μs | 339.1 μs | 18.59 μs |  0.75 |    0.09 |   23.56 KB |        0.74 |
| GetMessageAsync         |   195.2 μs | 147.8 μs |  8.10 μs |  1.00 |    0.05 |   31.95 KB |        1.00 |
