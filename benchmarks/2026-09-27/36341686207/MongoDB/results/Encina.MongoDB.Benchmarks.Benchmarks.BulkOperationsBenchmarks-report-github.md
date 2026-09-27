```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|-----------:|---------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 723.7 μs | 1,048.8 μs | 57.49 μs |  1.00 |    0.10 |  71.44 KB |        1.00 |
