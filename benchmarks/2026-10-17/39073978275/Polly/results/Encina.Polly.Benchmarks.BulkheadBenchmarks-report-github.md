```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 1,771.23 ns |    593.99 ns |   496.01 ns | 1,672.00 ns |  1.06 |    0.38 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,863.00 ns |    319.16 ns |   249.18 ns | 1,766.50 ns |  1.12 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    83.92 ns |     40.20 ns |    33.57 ns |    90.00 ns |  0.05 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,978.08 ns |    393.22 ns |   307.00 ns | 2,810.00 ns |  1.78 |    0.44 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,248.67 ns |    285.75 ns |   223.09 ns | 6,229.50 ns |  3.75 |    0.85 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,072.17 ns | 40,404.17 ns | 2,214.69 ns | 3,644.50 ns |  1.23 |    0.85 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,120.00 ns | 41,003.11 ns | 2,247.52 ns | 3,505.00 ns |  1.24 |    0.86 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   251.00 ns |  4,313.40 ns |   236.43 ns |   151.00 ns |  0.08 |    0.08 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 3,688.83 ns | 44,208.52 ns | 2,423.22 ns | 2,934.50 ns |  1.11 |    0.86 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 6,290.67 ns | 43,614.20 ns | 2,390.64 ns | 5,353.00 ns |  1.90 |    1.12 |    5528 B |        7.05 |
