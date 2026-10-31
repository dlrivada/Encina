```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 1.598 μs | 0.1198 μs | 0.0185 μs |  1.00 |    0.01 | 0.1011 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 1.605 μs | 0.1094 μs | 0.0284 μs |  1.00 |    0.02 | 0.0992 |   1.65 KB |        1.00 |
|                                         |            |                |             |          |           |           |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           | 1.580 μs | 0.2366 μs | 0.0130 μs |  1.00 |    0.01 | 0.1011 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           | 1.551 μs | 0.4732 μs | 0.0259 μs |  0.98 |    0.02 | 0.0992 |   1.65 KB |        1.00 |
