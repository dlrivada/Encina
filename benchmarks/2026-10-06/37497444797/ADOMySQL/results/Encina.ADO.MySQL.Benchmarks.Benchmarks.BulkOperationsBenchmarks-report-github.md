```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|-----------:|---------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 491.9 μs | 1,724.8 μs | 94.54 μs |  1.02 |    0.23 |  61.45 KB |        1.00 |
