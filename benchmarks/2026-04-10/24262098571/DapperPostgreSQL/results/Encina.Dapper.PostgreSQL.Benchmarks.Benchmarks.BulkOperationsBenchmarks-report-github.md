```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------- |-----:|------:|------:|--------:|------------:|
| BulkInsert_100Rows |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  BulkOperationsBenchmarks.BulkInsert_100Rows: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
