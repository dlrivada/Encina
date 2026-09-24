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
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 1.794 μs | 0.0632 μs | 0.0098 μs |  1.00 |    0.01 | 0.0153 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 1.804 μs | 0.0404 μs | 0.0063 μs |  1.01 |    0.01 | 0.0153 |   1.36 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |         |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 1.816 μs | 0.1445 μs | 0.0079 μs |  1.00 |    0.01 | 0.0153 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 1.812 μs | 0.6263 μs | 0.0343 μs |  1.00 |    0.02 | 0.0153 |   1.36 KB |        1.00 |
