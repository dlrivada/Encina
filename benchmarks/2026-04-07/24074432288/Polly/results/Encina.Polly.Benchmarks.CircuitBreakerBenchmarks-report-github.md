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
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 3           | 763.7 ns | 16.84 ns |  4.37 ns |  1.00 |    0.01 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 3           | 784.7 ns | 25.14 ns |  6.53 ns |  1.03 |    0.01 | 0.0534 |     896 B |        0.99 |
|                                         |            |                |             |             |          |          |          |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | MediumRun  | 15             | 2           | 10          | 787.9 ns | 16.76 ns | 23.50 ns |  1.00 |    0.04 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | MediumRun  | 15             | 2           | 10          | 773.7 ns |  2.90 ns |  4.16 ns |  0.98 |    0.03 | 0.0534 |     896 B |        0.99 |
