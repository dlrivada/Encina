```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 3           | 1.312 μs | 0.0382 μs | 0.0059 μs |  1.00 |    0.01 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 3           | 1.369 μs | 0.2577 μs | 0.0399 μs |  1.04 |    0.03 | 0.0191 |   1.65 KB |        1.00 |
|                                         |            |                |             |             |          |           |           |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | MediumRun  | 15             | 2           | 10          | 1.365 μs | 0.0390 μs | 0.0583 μs |  1.00 |    0.06 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | MediumRun  | 15             | 2           | 10          | 1.364 μs | 0.0497 μs | 0.0744 μs |  1.00 |    0.07 | 0.0191 |   1.65 KB |        1.00 |
