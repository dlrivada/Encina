```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
| MarkAsProcessedAsync    |   228.1 μs | 293.5 μs | 16.09 μs |  1.03 |    0.09 |   34.19 KB |        1.07 |
| GetExpiredMessagesAsync | 4,382.2 μs | 839.1 μs | 46.00 μs | 19.86 |    1.12 | 2189.73 KB |       68.55 |
| AddAsync                |   127.2 μs | 249.5 μs | 13.68 μs |  0.58 |    0.06 |   23.56 KB |        0.74 |
| GetMessageAsync         |   221.3 μs | 261.7 μs | 14.35 μs |  1.00 |    0.08 |   31.95 KB |        1.00 |
