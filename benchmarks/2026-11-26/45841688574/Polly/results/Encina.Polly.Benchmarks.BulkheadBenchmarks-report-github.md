```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  4,263.3 ns |    350.5 ns |   292.67 ns |  4,337.50 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,266.9 ns |    929.9 ns |   824.33 ns |  7,236.50 ns |  1.71 |    0.22 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    138.5 ns |    109.0 ns |    96.60 ns |     96.00 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,226.7 ns |    456.6 ns |   356.44 ns |  4,273.00 ns |  1.00 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,617.6 ns |  2,003.7 ns | 1,673.22 ns |  8,057.50 ns |  2.03 |    0.40 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |             |              |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  9,950.2 ns | 61,856.6 ns | 3,390.57 ns |  9,117.50 ns |  1.08 |    0.44 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  9,152.8 ns | 24,168.8 ns | 1,324.78 ns |  9,395.50 ns |  0.99 |    0.30 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    438.0 ns |  6,893.5 ns |   377.86 ns |    275.00 ns |  0.05 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,272.0 ns | 74,810.2 ns | 4,100.60 ns |  7,471.00 ns |  0.89 |    0.47 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 15,280.5 ns | 28,806.2 ns | 1,578.96 ns | 15,143.50 ns |  1.65 |    0.48 |    5528 B |        7.05 |
