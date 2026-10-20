```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|----------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 3.958 ms | 1.974 ms | 0.1082 ms |  1.00 |    0.03 | 103.03 KB |        1.00 |
