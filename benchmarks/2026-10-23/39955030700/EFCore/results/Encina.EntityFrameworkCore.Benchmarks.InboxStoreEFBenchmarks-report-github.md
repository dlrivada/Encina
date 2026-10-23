```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   196.2 μs |   508.4 μs | 27.87 μs |  0.84 |    0.12 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 2,519.8 μs | 1,029.1 μs | 56.41 μs | 10.77 |    0.78 | 2189.73 KB |       68.55 |
| AddAsync                |   107.8 μs |   162.2 μs |  8.89 μs |  0.46 |    0.05 |   23.56 KB |        0.74 |
| GetMessageAsync         |   235.1 μs |   357.1 μs | 19.58 μs |  1.00 |    0.10 |   31.95 KB |        1.00 |
