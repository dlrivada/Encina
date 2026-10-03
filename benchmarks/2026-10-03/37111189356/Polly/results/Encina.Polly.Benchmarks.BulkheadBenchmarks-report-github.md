```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error      | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5.027 μs |  0.8799 μs | 0.7800 μs | 4.8575 μs |  1.02 |    0.21 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5.019 μs |  0.6382 μs | 0.5330 μs | 4.8830 μs |  1.02 |    0.18 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           | 1.484 μs |  1.3764 μs | 1.2875 μs | 1.9460 μs |  0.30 |    0.26 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 4.867 μs |  0.6047 μs | 0.5361 μs | 4.8040 μs |  0.99 |    0.18 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 9.957 μs |  2.4732 μs | 2.3134 μs | 9.4065 μs |  2.02 |    0.54 |    5528 B |        7.05 |
|                                |            |                |             |             |          |            |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 9.191 μs | 57.9317 μs | 3.1754 μs | 8.2895 μs |  1.08 |    0.44 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 8.568 μs | 59.0035 μs | 3.2342 μs | 7.6685 μs |  1.00 |    0.44 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           | 1.521 μs | 36.5283 μs | 2.0022 μs | 0.4240 μs |  0.18 |    0.22 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 6.707 μs | 55.7501 μs | 3.0559 μs | 6.2150 μs |  0.79 |    0.39 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9.723 μs | 57.0052 μs | 3.1246 μs | 8.2400 μs |  1.14 |    0.45 |    5528 B |        7.05 |
