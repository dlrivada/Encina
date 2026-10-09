```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 2.431 μs | 0.0266 μs | 0.0041 μs |  1.00 | 0.0992 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 2.404 μs | 0.0199 μs | 0.0052 μs |  0.99 | 0.0992 |   1.65 KB |        1.00 |
|                                         |            |                |             |          |           |           |       |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           | 2.321 μs | 0.1282 μs | 0.0070 μs |  1.00 | 0.0992 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           | 2.430 μs | 0.1126 μs | 0.0062 μs |  1.05 | 0.0992 |   1.65 KB |        1.00 |
