```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 2.367 μs | 0.0330 μs | 0.0051 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 2.366 μs | 0.0264 μs | 0.0069 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 2.371 μs | 0.2707 μs | 0.0148 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 2.411 μs | 0.1903 μs | 0.0104 μs |  1.02 | 0.0992 |   1.63 KB |        1.00 |
