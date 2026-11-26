```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,490.4 ns |     266.73 ns |   222.73 ns | 5,518.5 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,837.5 ns |     672.27 ns |   561.38 ns | 5,889.0 ns |  1.06 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    387.5 ns |      60.79 ns |    47.46 ns |   406.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,429.1 ns |     373.31 ns |   349.20 ns | 5,348.0 ns |  0.99 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,557.4 ns |     308.59 ns |   273.56 ns | 8,548.0 ns |  1.56 |    0.08 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 11,386.3 ns | 161,740.04 ns | 8,865.52 ns | 6,459.0 ns |  1.39 |    1.21 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  6,587.0 ns |  13,547.46 ns |   742.58 ns | 6,901.0 ns |  0.80 |    0.38 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    644.7 ns |   2,456.50 ns |   134.65 ns |   701.0 ns |  0.08 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,723.2 ns |  11,545.86 ns |   632.87 ns | 5,512.5 ns |  0.70 |    0.33 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           |  9,134.3 ns |  10,615.44 ns |   581.87 ns | 8,834.0 ns |  1.11 |    0.52 |    5528 B |        7.05 |
