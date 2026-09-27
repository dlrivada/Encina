```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 215.3 μs | 305.5 μs | 16.75 μs |  1.00 |    0.10 |  10.13 KB |        1.00 |
