```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoCircuitBreakerAttribute_Baseline      | Job-NTRUNJ | 5              | Default     | 1.978 μs | 0.0485 μs | 0.0126 μs |  1.00 |    0.01 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | Job-NTRUNJ | 5              | Default     | 2.016 μs | 0.1787 μs | 0.0464 μs |  1.02 |    0.02 | 0.0191 |   1.65 KB |        1.00 |
|                                         |            |                |             |          |           |           |       |         |        |           |             |
| NoCircuitBreakerAttribute_Baseline      | ShortRun   | 3              | 1           | 2.054 μs | 1.3000 μs | 0.0713 μs |  1.00 |    0.04 | 0.0191 |   1.66 KB |        1.00 |
| WithCircuitBreakerAttribute_ClosedState | ShortRun   | 3              | 1           | 2.034 μs | 1.0934 μs | 0.0599 μs |  0.99 |    0.04 | 0.0191 |   1.65 KB |        1.00 |
