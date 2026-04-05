```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                  | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|--------:|------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        812.7 ns | 13.04 ns | 2.02 ns |  1.00 | 0.0534 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        850.8 ns | 18.12 ns | 4.71 ns |  1.05 | 0.0534 |     896 B |        0.99 |
|                                         |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 42,688,965.0 ns |       NA | 0.00 ns |  1.00 |      - |    1656 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 42,185,766.0 ns |       NA | 0.00 ns |  0.99 |      - |    1640 B |        0.99 |
