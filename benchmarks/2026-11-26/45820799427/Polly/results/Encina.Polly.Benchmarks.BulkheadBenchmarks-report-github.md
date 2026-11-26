```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error        | StdDev      | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-------------:|-------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  2,580.08 ns |    962.12 ns |   803.41 ns |  2,273.00 ns |  1.08 |    0.43 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  2,845.81 ns |  1,352.60 ns | 1,129.48 ns |  2,320.50 ns |  1.19 |    0.56 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |     64.38 ns |     84.50 ns |    70.56 ns |     35.50 ns |  0.03 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  2,799.79 ns |  1,254.52 ns | 1,112.10 ns |  2,233.00 ns |  1.17 |    0.55 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  7,815.50 ns |  1,414.63 ns | 1,181.28 ns |  8,100.50 ns |  3.27 |    0.96 |    5528 B |        7.05 |
|                                |            |                |             |             |              |              |             |              |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,154.33 ns | 87,385.40 ns | 4,789.89 ns |  4,851.00 ns |  1.56 |    1.61 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,078.67 ns | 96,852.62 ns | 5,308.82 ns |  6,200.00 ns |  1.79 |    1.81 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    246.33 ns |  5,309.96 ns |   291.06 ns |    171.50 ns |  0.06 |    0.09 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,282.83 ns | 66,961.31 ns | 3,670.38 ns |  4,047.50 ns |  1.34 |    1.30 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,328.33 ns | 83,301.74 ns | 4,566.05 ns | 11,275.00 ns |  3.12 |    2.37 |    5528 B |        7.05 |
