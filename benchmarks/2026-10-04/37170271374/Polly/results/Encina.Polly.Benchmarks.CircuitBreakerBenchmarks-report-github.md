```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 3           | 1.763 μs | 0.0535 μs | 0.0083 μs | 1.763 μs |  1.00 | 0.0801 |   1.33 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 3           | 1.809 μs | 0.0342 μs | 0.0053 μs | 1.811 μs |  1.03 | 0.0801 |   1.32 KB |        0.99 |
|                                         |            |                |             |             |          |           |           |          |       |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | MediumRun  | 15             | 2           | 10          | 1.805 μs | 0.0116 μs | 0.0170 μs | 1.815 μs |  1.00 | 0.0801 |   1.33 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | MediumRun  | 15             | 2           | 10          | 1.819 μs | 0.0131 μs | 0.0188 μs | 1.812 μs |  1.01 | 0.0801 |   1.32 KB |        0.99 |
