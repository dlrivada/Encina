```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,064.4 ns |    912.73 ns |   809.11 ns |  1.02 |    0.21 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,451.3 ns |    314.61 ns |   262.71 ns |  1.10 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   359.3 ns |     32.36 ns |    27.03 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,227.9 ns |    264.81 ns |   221.13 ns |  1.05 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,262.3 ns |    198.82 ns |   166.02 ns |  1.67 |    0.23 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 6,804.0 ns | 22,580.98 ns | 1,237.74 ns |  1.02 |    0.24 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 6,034.7 ns |  9,395.15 ns |   514.98 ns |  0.91 |    0.17 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   394.3 ns |  1,836.49 ns |   100.66 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 6,542.0 ns |  8,907.48 ns |   488.25 ns |  0.99 |    0.18 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,396.3 ns | 15,643.23 ns |   857.46 ns |  1.41 |    0.27 |    5528 B |        7.05 |
