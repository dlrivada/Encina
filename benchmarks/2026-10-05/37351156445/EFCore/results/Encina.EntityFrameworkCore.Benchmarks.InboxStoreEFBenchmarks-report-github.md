```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 138.49 μs |  38.25 μs |  2.097 μs |  1.08 |    0.05 |  11.97 KB |        1.16 |
| GetExpiredMessagesAsync | 891.14 μs | 922.03 μs | 50.540 μs |  6.98 |    0.46 | 287.91 KB |       27.88 |
| AddAsync                |  47.09 μs |  22.77 μs |  1.248 μs |  0.37 |    0.02 |   2.85 KB |        0.28 |
| GetMessageAsync         | 127.94 μs | 118.66 μs |  6.504 μs |  1.00 |    0.06 |  10.33 KB |        1.00 |
