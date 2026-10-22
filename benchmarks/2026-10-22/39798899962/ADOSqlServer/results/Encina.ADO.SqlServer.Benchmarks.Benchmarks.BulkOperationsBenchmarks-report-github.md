```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|----------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 4.046 ms | 3.042 ms | 0.1668 ms |  1.00 |    0.05 | 103.08 KB |        1.00 |
