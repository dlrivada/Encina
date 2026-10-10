```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,444.5 ns |     420.87 ns |   351.45 ns |  5,409.0 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,354.0 ns |     386.70 ns |   342.80 ns |  5,332.5 ns |  0.99 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    409.2 ns |      45.26 ns |    37.80 ns |    411.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,225.4 ns |     207.23 ns |   183.71 ns |  5,187.5 ns |  0.96 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,121.6 ns |   1,508.39 ns | 1,337.15 ns | 10,375.5 ns |  1.87 |    0.26 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,777.3 ns |   6,595.81 ns |   361.54 ns |  5,637.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,567.7 ns |  93,957.95 ns | 5,150.15 ns |  8,838.0 ns |  1.83 |    0.78 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    337.0 ns |   1,000.41 ns |    54.84 ns |    311.0 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,906.2 ns | 110,178.83 ns | 6,039.27 ns |  5,677.5 ns |  1.55 |    0.91 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           |  8,546.0 ns |   8,660.06 ns |   474.69 ns |  8,282.0 ns |  1.48 |    0.11 |    5528 B |        7.05 |
