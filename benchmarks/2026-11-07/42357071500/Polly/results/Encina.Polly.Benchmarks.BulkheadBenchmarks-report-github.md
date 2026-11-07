```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,327.4 ns |    183.29 ns |   162.48 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,528.0 ns |    473.82 ns |   443.21 ns |  1.04 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   371.4 ns |     50.20 ns |    44.50 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,424.5 ns |    394.42 ns |   368.94 ns |  1.02 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,058.4 ns |    285.02 ns |   252.67 ns |  1.51 |    0.06 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,996.0 ns | 13,718.84 ns |   751.98 ns |  1.01 |    0.16 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,694.0 ns |  9,068.56 ns |   497.08 ns |  0.96 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   425.5 ns |  1,579.95 ns |    86.60 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,605.3 ns | 13,193.39 ns |   723.17 ns |  0.95 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,843.0 ns | 19,838.43 ns | 1,087.41 ns |  1.49 |    0.24 |    5528 B |        7.05 |
