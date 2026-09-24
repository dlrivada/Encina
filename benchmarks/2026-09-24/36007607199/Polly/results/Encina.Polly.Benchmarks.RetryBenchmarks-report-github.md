```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 1.739 μs | 0.0942 μs | 0.0245 μs |  1.00 |    0.02 | 0.0153 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 1.662 μs | 0.0815 μs | 0.0126 μs |  0.96 |    0.01 | 0.0153 |   1.36 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |         |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 1.748 μs | 0.3481 μs | 0.0191 μs |  1.00 |    0.01 | 0.0153 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 1.688 μs | 0.6637 μs | 0.0364 μs |  0.97 |    0.02 | 0.0153 |   1.36 KB |        1.00 |
