```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,059.2 ns |     181.15 ns |   151.27 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,904.7 ns |     286.32 ns |   253.82 ns |  0.98 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    569.2 ns |      92.56 ns |    82.05 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,868.3 ns |   1,492.38 ns | 1,246.21 ns |  1.12 |    0.17 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,282.4 ns |     747.99 ns |   583.98 ns |  1.60 |    0.09 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,087.3 ns |   5,135.32 ns |   281.48 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,492.8 ns |   9,295.43 ns |   509.51 ns |  1.06 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    866.8 ns |   5,288.49 ns |   289.88 ns |  0.12 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,984.8 ns |   7,503.50 ns |   411.29 ns |  0.99 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 16,488.0 ns | 101,691.33 ns | 5,574.04 ns |  2.33 |    0.69 |    5528 B |        7.05 |
