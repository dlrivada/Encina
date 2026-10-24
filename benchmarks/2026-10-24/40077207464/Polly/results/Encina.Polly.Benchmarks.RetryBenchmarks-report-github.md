```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 1.933 μs | 0.0381 μs | 0.0059 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 1.969 μs | 0.0213 μs | 0.0055 μs |  1.02 | 0.0992 |   1.63 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 1.956 μs | 0.2040 μs | 0.0112 μs |  1.00 | 0.0992 |   1.63 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 1.942 μs | 0.1056 μs | 0.0058 μs |  0.99 | 0.0992 |   1.63 KB |        1.00 |
