```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                                  | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        861.5 ns | 3.96 ns | 1.03 ns |  1.00 | 0.0353 |     904 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | Default     | 16           | 3           |        861.0 ns | 4.94 ns | 0.77 ns |  1.00 | 0.0353 |     896 B |        0.99 |
|                                         |            |                |             |             |              |             |                 |         |         |       |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 39,600,816.0 ns |      NA | 0.00 ns |  1.00 |      - |    1656 B |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 40,518,198.0 ns |      NA | 0.00 ns |  1.02 |      - |    1640 B |        0.99 |
