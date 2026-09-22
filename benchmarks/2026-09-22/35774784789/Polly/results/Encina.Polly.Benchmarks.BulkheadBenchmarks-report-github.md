```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,758.7 ns |    293.65 ns |   245.21 ns |  6,703.0 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,085.9 ns |    510.90 ns |   452.90 ns |  6,893.5 ns |  1.05 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    466.2 ns |     22.01 ns |    18.38 ns |    471.0 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,639.8 ns |    464.62 ns |   411.87 ns |  7,780.0 ns |  1.13 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,342.7 ns |    103.52 ns |    91.77 ns | 10,325.0 ns |  1.53 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,237.3 ns |  2,851.71 ns |   156.31 ns |  7,214.0 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,318.8 ns | 18,835.49 ns | 1,032.44 ns |  6,857.5 ns |  1.01 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    692.8 ns |  7,157.52 ns |   392.33 ns |    545.5 ns |  0.10 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,466.7 ns | 14,818.04 ns |   812.23 ns |  7,283.0 ns |  1.03 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,633.3 ns | 13,680.62 ns |   749.88 ns | 11,376.0 ns |  1.61 |    0.09 |    5528 B |        7.05 |
