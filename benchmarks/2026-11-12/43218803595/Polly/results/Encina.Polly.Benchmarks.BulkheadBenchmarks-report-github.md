```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,206.4 ns |    744.55 ns |   660.03 ns | 2,027.5 ns |  1.08 |    0.42 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 3,594.5 ns |  1,784.34 ns | 1,490.00 ns | 3,140.0 ns |  1.75 |    0.85 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   114.9 ns |     75.00 ns |    66.49 ns |   130.5 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,496.9 ns |    542.65 ns |   481.04 ns | 2,353.5 ns |  1.22 |    0.39 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,079.9 ns |    511.27 ns |   453.23 ns | 5,989.0 ns |  2.97 |    0.78 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,535.0 ns | 45,574.45 ns | 2,498.09 ns | 3,690.0 ns |  1.20 |    0.79 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,976.0 ns | 42,708.46 ns | 2,340.99 ns | 4,888.0 ns |  1.59 |    0.87 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   644.3 ns | 17,840.80 ns |   977.91 ns |   110.0 ns |  0.17 |    0.25 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 4,575.3 ns | 51,883.06 ns | 2,843.89 ns | 3,991.0 ns |  1.21 |    0.86 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 7,554.7 ns | 37,961.06 ns | 2,080.77 ns | 7,070.0 ns |  2.01 |    0.97 |    5528 B |        7.05 |
