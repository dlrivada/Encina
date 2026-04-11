```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 3           | 781.2 ns | 18.70 ns |  2.89 ns |  1.00 |    0.00 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 3           | 790.6 ns | 76.63 ns | 11.86 ns |  1.01 |    0.01 | 0.0534 |     896 B |        0.99 |
|                                         |            |                |             |             |          |          |          |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | MediumRun  | 15             | 2           | 10          | 783.8 ns |  7.38 ns | 11.05 ns |  1.00 |    0.02 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | MediumRun  | 15             | 2           | 10          | 825.3 ns |  9.64 ns | 14.42 ns |  1.05 |    0.02 | 0.0534 |     896 B |        0.99 |
