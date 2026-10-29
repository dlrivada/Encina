```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------- |-----:|------:|------:|--------:|------------:|
| BulkInsert_100Rows |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  BulkOperationsBenchmarks.BulkInsert_100Rows: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
