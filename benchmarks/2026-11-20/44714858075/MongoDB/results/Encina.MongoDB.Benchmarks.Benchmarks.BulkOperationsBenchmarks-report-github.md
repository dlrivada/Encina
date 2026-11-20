```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host] : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------- |-----:|------:|------:|--------:|------------:|
| BulkInsert_100Rows |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  BulkOperationsBenchmarks.BulkInsert_100Rows: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
