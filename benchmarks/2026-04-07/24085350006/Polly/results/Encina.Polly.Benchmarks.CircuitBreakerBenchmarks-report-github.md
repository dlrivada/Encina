```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 854.8 ns | 20.79 ns | 5.40 ns |  1.00 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 847.5 ns |  7.32 ns | 1.13 ns |  0.99 | 0.0534 |     896 B |        0.99 |
|                                         |            |                |             |          |          |         |       |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           | 824.6 ns | 32.27 ns | 1.77 ns |  1.00 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           | 822.5 ns | 79.37 ns | 4.35 ns |  1.00 | 0.0534 |     896 B |        0.99 |
