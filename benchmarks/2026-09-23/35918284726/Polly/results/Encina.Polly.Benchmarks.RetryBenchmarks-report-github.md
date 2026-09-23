```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 1.804 μs | 0.0529 μs | 0.0137 μs |  1.00 | 0.0820 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 1.801 μs | 0.0602 μs | 0.0093 μs |  1.00 | 0.0820 |   1.36 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 1.807 μs | 0.3363 μs | 0.0184 μs |  1.00 | 0.0820 |   1.36 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 1.808 μs | 0.4141 μs | 0.0227 μs |  1.00 | 0.0820 |   1.36 KB |        1.00 |
