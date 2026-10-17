```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,147.9 ns |     320.74 ns |   284.33 ns | 5,072.5 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,476.8 ns |     469.49 ns |   439.16 ns | 5,448.0 ns |  1.07 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   207.2 ns |      40.75 ns |    31.82 ns |   211.0 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,123.0 ns |     216.55 ns |   180.83 ns | 5,092.5 ns |  1.00 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,488.0 ns |     143.14 ns |   111.76 ns | 8,477.0 ns |  1.65 |    0.09 |    5528 B |        7.05 |
|                                |            |                |             |             |            |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,608.3 ns |   9,575.79 ns |   524.88 ns | 5,378.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,868.5 ns |  14,293.48 ns |   783.47 ns | 5,678.5 ns |  1.05 |    0.15 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   451.0 ns |     547.31 ns |    30.00 ns |   451.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 9,848.0 ns | 129,542.43 ns | 7,100.66 ns | 5,789.0 ns |  1.77 |    1.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,594.2 ns |  11,922.58 ns |   653.52 ns | 8,377.5 ns |  1.54 |    0.16 |    5528 B |        7.05 |
