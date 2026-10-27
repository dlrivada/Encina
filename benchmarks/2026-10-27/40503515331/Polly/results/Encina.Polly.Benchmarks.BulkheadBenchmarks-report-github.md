```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error        | StdDev      | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-------------:|-------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  4,263.25 ns |    358.32 ns |   279.76 ns |  4,235.00 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  4,375.79 ns |  1,982.46 ns | 1,757.40 ns |  3,341.50 ns |  1.03 |    0.41 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |     66.65 ns |     12.34 ns |    10.30 ns |     66.50 ns |  0.02 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,554.50 ns |  1,200.08 ns |   936.95 ns |  6,790.00 ns |  1.54 |    0.23 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  7,529.15 ns |    491.94 ns |   410.79 ns |  7,442.00 ns |  1.77 |    0.14 |    5528 B |        7.05 |
|                                |            |                |             |             |              |              |             |              |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,680.17 ns | 70,103.60 ns | 3,842.62 ns |  6,001.50 ns |  1.15 |    0.67 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,747.00 ns | 49,460.06 ns | 2,711.07 ns |  7,188.00 ns |  1.16 |    0.55 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    425.50 ns |  6,955.24 ns |   381.24 ns |    217.50 ns |  0.06 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,464.33 ns | 31,237.37 ns | 1,712.23 ns |  8,071.00 ns |  1.27 |    0.51 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 14,620.83 ns | 66,006.45 ns | 3,618.04 ns | 12,914.50 ns |  2.20 |    0.92 |    5528 B |        7.05 |
