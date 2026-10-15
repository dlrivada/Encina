```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,248.5 ns |     281.24 ns |   234.85 ns |  7,154.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,185.6 ns |     365.47 ns |   305.18 ns |  7,118.0 ns |  0.99 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    562.9 ns |      53.09 ns |    41.45 ns |    556.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,544.3 ns |     300.78 ns |   251.16 ns |  7,564.0 ns |  1.04 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,488.0 ns |     227.07 ns |   201.30 ns | 14,457.5 ns |  2.00 |    0.07 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,765.0 ns |   8,231.56 ns |   451.20 ns |  7,505.0 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 12,328.3 ns | 140,009.78 ns | 7,674.41 ns |  8,351.0 ns |  1.59 |    0.86 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    525.0 ns |     390.01 ns |    21.38 ns |    532.0 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 10,622.8 ns | 112,021.85 ns | 6,140.30 ns |  7,153.5 ns |  1.37 |    0.69 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,200.8 ns |   5,720.22 ns |   313.54 ns | 11,350.5 ns |  1.45 |    0.08 |    5528 B |        7.05 |
