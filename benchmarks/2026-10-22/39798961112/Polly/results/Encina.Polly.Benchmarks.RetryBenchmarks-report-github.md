```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 2.435 μs | 0.0271 μs | 0.0042 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 2.280 μs | 0.0181 μs | 0.0047 μs |  0.94 | 0.0992 |   1.63 KB |        1.00 |
|                                    |            |                |             |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 2.407 μs | 0.0103 μs | 0.0147 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 2.367 μs | 0.0190 μs | 0.0279 μs |  0.98 | 0.0992 |   1.63 KB |        1.00 |
