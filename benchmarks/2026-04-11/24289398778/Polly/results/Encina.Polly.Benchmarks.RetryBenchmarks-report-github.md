```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|---------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 765.2 ns | 8.44 ns |  1.31 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 751.4 ns | 5.58 ns |  1.45 ns |  0.98 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |          |         |          |       |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 780.0 ns | 0.96 ns |  1.37 ns |  1.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 788.2 ns | 7.86 ns | 11.03 ns |  1.01 | 0.0525 |     880 B |        1.00 |
