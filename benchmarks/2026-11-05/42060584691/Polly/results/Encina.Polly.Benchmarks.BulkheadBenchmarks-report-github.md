```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,828.4 ns |    924.94 ns |   819.93 ns |  5,576.0 ns |  1.02 |    0.19 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,730.9 ns |    322.22 ns |   269.07 ns |  7,659.0 ns |  1.35 |    0.17 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    207.4 ns |     51.45 ns |    42.97 ns |    190.5 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,504.3 ns |    307.02 ns |   256.38 ns |  4,485.5 ns |  0.79 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,647.9 ns |    935.44 ns |   829.24 ns | 14,644.5 ns |  2.56 |    0.35 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,639.5 ns | 42,411.78 ns | 2,324.73 ns |  7,865.5 ns |  1.05 |    0.33 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,269.7 ns | 58,883.67 ns | 3,227.61 ns |  6,950.0 ns |  1.00 |    0.40 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    625.7 ns |  5,642.42 ns |   309.28 ns |    500.0 ns |  0.08 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,195.3 ns | 70,523.76 ns | 3,865.65 ns |  6,880.0 ns |  0.99 |    0.46 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,970.8 ns | 82,185.11 ns | 4,504.84 ns | 12,427.5 ns |  1.69 |    0.60 |    5528 B |        7.05 |
