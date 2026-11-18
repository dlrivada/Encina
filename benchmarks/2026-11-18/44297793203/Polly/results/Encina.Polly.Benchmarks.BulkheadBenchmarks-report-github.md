```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,027.1 ns |    119.37 ns |   105.82 ns |  7,018.5 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,948.4 ns |    211.63 ns |   187.60 ns |  7,929.5 ns |  1.13 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    550.5 ns |     62.31 ns |    58.29 ns |    531.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,915.5 ns |  1,380.14 ns | 1,152.48 ns |  7,734.0 ns |  1.13 |    0.16 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,580.7 ns |    293.31 ns |   244.92 ns | 11,591.5 ns |  1.65 |    0.04 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  9,791.5 ns | 82,260.89 ns | 4,509.00 ns |  7,253.5 ns |  1.13 |    0.59 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,337.7 ns |  7,430.60 ns |   407.30 ns |  7,204.0 ns |  0.84 |    0.27 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    542.5 ns |  1,013.64 ns |    55.56 ns |    545.5 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,116.8 ns |  6,414.77 ns |   351.62 ns |  6,993.5 ns |  0.82 |    0.26 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,136.5 ns | 57,668.35 ns | 3,161.00 ns | 11,316.5 ns |  1.51 |    0.58 |    5528 B |        7.05 |
