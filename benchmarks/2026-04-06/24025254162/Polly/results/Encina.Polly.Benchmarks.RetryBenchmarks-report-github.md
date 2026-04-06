```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 804.6 ns |  17.68 ns |  4.59 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 849.1 ns |  19.08 ns |  4.96 ns |  1.06 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |          |           |          |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 856.5 ns |  44.36 ns |  2.43 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 825.2 ns | 225.81 ns | 12.38 ns |  0.96 | 0.0525 |     880 B |        1.00 |
