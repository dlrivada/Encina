```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 3           | 1.535 μs | 0.4441 μs | 0.1153 μs |  1.00 |    0.10 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 3           | 1.437 μs | 0.0276 μs | 0.0072 μs |  0.94 |    0.06 | 0.0191 |   1.65 KB |        1.00 |
|                                         |            |                |             |             |          |           |           |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | MediumRun  | 15             | 2           | 10          | 1.435 μs | 0.0252 μs | 0.0345 μs |  1.00 |    0.03 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | MediumRun  | 15             | 2           | 10          | 1.420 μs | 0.0121 μs | 0.0170 μs |  0.99 |    0.03 | 0.0191 |   1.65 KB |        1.00 |
