```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,426.00 ns |    793.52 ns |   703.44 ns | 3,305.500 ns | 1.046 |    0.33 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,903.54 ns |    509.07 ns |   425.10 ns | 1,778.000 ns | 0.581 |    0.19 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    17.14 ns |     24.00 ns |    21.28 ns |     5.000 ns | 0.005 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,933.71 ns |    808.62 ns |   716.82 ns | 1,882.500 ns | 0.591 |    0.26 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 5,966.36 ns |    464.81 ns |   412.04 ns | 5,899.000 ns | 1.822 |    0.45 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |              |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,135.50 ns | 41,434.12 ns | 2,271.14 ns | 4,571.500 ns |  1.13 |    0.61 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,942.17 ns | 26,549.19 ns | 1,455.25 ns | 4,471.500 ns |  1.09 |    0.49 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   222.17 ns |  4,105.18 ns |   225.02 ns |   145.500 ns |  0.05 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 3,946.00 ns | 34,841.63 ns | 1,909.79 ns | 3,125.000 ns |  0.87 |    0.49 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 7,773.67 ns | 43,825.24 ns | 2,402.21 ns | 7,917.000 ns |  1.72 |    0.78 |    5528 B |        7.05 |
