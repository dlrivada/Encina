```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 2.563 μs | 0.0318 μs | 0.0082 μs |  1.00 |    0.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 2.607 μs | 0.0712 μs | 0.0110 μs |  1.02 |    0.00 | 0.0992 |   1.63 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |         |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 2.598 μs | 0.6150 μs | 0.0337 μs |  1.00 |    0.02 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 2.666 μs | 0.3479 μs | 0.0191 μs |  1.03 |    0.01 | 0.0992 |   1.63 KB |        1.00 |
