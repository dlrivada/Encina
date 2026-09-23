```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 2.172 μs | 0.0295 μs | 0.0077 μs |  1.00 |    0.00 | 0.0839 |   1.38 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 2.185 μs | 0.0437 μs | 0.0113 μs |  1.01 |    0.01 | 0.0839 |   1.38 KB |        0.99 |
|                                         |            |                |             |          |           |           |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           | 2.106 μs | 0.0186 μs | 0.0010 μs |  1.00 |    0.00 | 0.0839 |   1.38 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           | 2.130 μs | 0.6727 μs | 0.0369 μs |  1.01 |    0.02 | 0.0839 |   1.38 KB |        0.99 |
