```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 2.082 μs | 0.0300 μs | 0.0046 μs | 2.083 μs |  1.00 |    0.00 | 0.0763 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 2.080 μs | 0.0343 μs | 0.0053 μs | 2.081 μs |  1.00 |    0.00 | 0.0763 |    1.3 KB |        1.00 |
|                                    |            |                |             |             |          |           |           |          |       |         |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 2.114 μs | 0.0340 μs | 0.0477 μs | 2.155 μs |  1.00 |    0.03 | 0.0763 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 2.101 μs | 0.0153 μs | 0.0225 μs | 2.092 μs |  0.99 |    0.02 | 0.0763 |    1.3 KB |        1.00 |
