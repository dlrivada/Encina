```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,166.0 ns |    934.38 ns |   780.25 ns |  6,267.5 ns |  1.02 |    0.18 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,088.8 ns |    782.03 ns |   610.55 ns |  6,300.0 ns |  1.00 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    174.5 ns |     33.85 ns |    28.27 ns |    183.0 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,100.1 ns |    494.06 ns |   412.56 ns |  3,898.0 ns |  0.68 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 12,225.1 ns |  1,225.22 ns |   956.57 ns | 12,245.5 ns |  2.01 |    0.31 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,701.5 ns | 74,788.38 ns | 4,099.40 ns |  7,248.5 ns |  1.14 |    0.63 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  9,388.0 ns | 52,067.65 ns | 2,854.00 ns |  8,100.0 ns |  1.23 |    0.55 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    584.0 ns | 10,202.35 ns |   559.23 ns |    303.0 ns |  0.08 |    0.07 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,278.5 ns | 79,520.54 ns | 4,358.79 ns |  6,897.5 ns |  1.09 |    0.65 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 14,158.3 ns | 49,462.80 ns | 2,711.22 ns | 12,836.0 ns |  1.86 |    0.73 |    5528 B |        7.05 |
