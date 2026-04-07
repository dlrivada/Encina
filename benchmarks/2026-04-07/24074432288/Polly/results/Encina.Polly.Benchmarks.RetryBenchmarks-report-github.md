```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 820.6 ns |  8.65 ns | 1.34 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 855.3 ns | 22.64 ns | 5.88 ns |  1.04 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |          |          |         |       |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 866.4 ns |  3.28 ns | 4.81 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 823.0 ns |  5.82 ns | 8.53 ns |  0.95 | 0.0525 |     880 B |        1.00 |
