```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,132.6 ns |  1,565.43 ns | 1,387.71 ns | 3,119.5 ns |  1.21 |    0.78 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 3,336.0 ns |    843.17 ns |   658.29 ns | 3,440.5 ns |  1.29 |    0.63 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   107.9 ns |     73.79 ns |    61.62 ns |   110.0 ns |  0.04 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 3,171.4 ns |    421.10 ns |   351.64 ns | 3,020.0 ns |  1.22 |    0.56 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 5,437.0 ns |    606.33 ns |   506.32 ns | 5,323.0 ns |  2.10 |    0.96 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 6,931.5 ns | 64,914.37 ns | 3,558.18 ns | 5,903.5 ns |  1.18 |    0.74 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,084.2 ns | 43,460.29 ns | 2,382.20 ns | 3,279.5 ns |  0.70 |    0.47 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   764.7 ns | 19,236.73 ns | 1,054.43 ns |   290.0 ns |  0.13 |    0.17 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,412.0 ns | 67,540.66 ns | 3,702.13 ns | 3,556.0 ns |  0.92 |    0.69 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,506.5 ns | 68,101.86 ns | 3,732.89 ns | 7,860.5 ns |  1.62 |    0.88 |    5528 B |        7.05 |
