```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,138.1 ns |     535.1 ns |   474.4 ns |  5,976.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,461.3 ns |     755.4 ns |   669.6 ns |  5,388.5 ns |  0.89 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |  1,361.7 ns |     717.8 ns |   671.4 ns |  1,682.5 ns |  0.22 |    0.11 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,934.3 ns |     376.0 ns |   314.0 ns |  5,817.5 ns |  0.97 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 12,173.2 ns |     768.2 ns |   681.0 ns | 11,997.5 ns |  1.99 |    0.18 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,088.7 ns |  53,008.9 ns | 2,905.6 ns |  6,396.0 ns |  1.11 |    0.56 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,318.7 ns |  43,362.4 ns | 2,376.8 ns |  8,519.0 ns |  1.31 |    0.55 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    539.8 ns |   8,035.5 ns |   440.5 ns |    312.5 ns |  0.08 |    0.07 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 11,792.5 ns | 108,563.2 ns | 5,950.7 ns | 10,587.5 ns |  1.85 |    1.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 22,435.0 ns | 171,344.2 ns | 9,392.0 ns | 27,681.0 ns |  3.53 |    1.78 |    5528 B |        7.05 |
