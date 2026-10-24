```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,584.62 ns |     305.03 ns |   254.71 ns | 1,522.00 ns |  1.02 |    0.21 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,745.00 ns |     722.09 ns |   640.12 ns | 1,431.50 ns |  1.12 |    0.43 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    44.50 ns |      36.59 ns |    32.44 ns |    35.00 ns |  0.03 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,072.57 ns |   1,536.29 ns | 1,361.88 ns | 1,296.50 ns |  1.34 |    0.87 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,754.00 ns |     384.09 ns |   320.73 ns | 6,830.00 ns |  4.35 |    0.62 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,128.00 ns |  69,754.05 ns | 3,823.46 ns | 3,976.00 ns |  1.46 |    1.39 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 9,590.50 ns | 107,898.28 ns | 5,914.27 ns | 7,681.50 ns |  2.73 |    2.34 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           | 1,282.33 ns |  22,867.27 ns | 1,253.43 ns | 1,202.00 ns |  0.37 |    0.42 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,539.50 ns |  49,063.08 ns | 2,689.31 ns | 4,741.50 ns |  1.58 |    1.22 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 7,584.50 ns |  69,445.61 ns | 3,806.55 ns | 6,209.50 ns |  2.16 |    1.69 |    5528 B |        7.05 |
