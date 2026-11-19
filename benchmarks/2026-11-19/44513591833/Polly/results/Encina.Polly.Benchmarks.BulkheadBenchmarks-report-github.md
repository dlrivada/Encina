```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 2,840.6 ns |    710.8 ns |   593.5 ns | 2,985.00 ns |  1.05 |    0.33 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 2,296.1 ns |    705.8 ns |   551.1 ns | 2,138.50 ns |  0.85 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   186.6 ns |    429.6 ns |   358.8 ns |    30.00 ns |  0.07 |    0.13 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 2,687.2 ns |  2,001.0 ns | 1,773.9 ns | 1,922.00 ns |  0.99 |    0.69 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,478.2 ns |    673.4 ns |   525.7 ns | 6,309.50 ns |  2.39 |    0.61 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 4,002.5 ns | 38,842.0 ns | 2,129.1 ns | 3,064.50 ns |  1.18 |    0.72 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 6,803.5 ns | 25,259.4 ns | 1,384.6 ns | 6,890.50 ns |  2.00 |    0.83 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   237.3 ns |  4,798.5 ns |   263.0 ns |    90.00 ns |  0.07 |    0.08 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,525.3 ns | 13,247.8 ns |   726.2 ns | 5,508.00 ns |  1.62 |    0.63 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 7,437.8 ns | 51,180.9 ns | 2,805.4 ns | 8,212.50 ns |  2.18 |    1.11 |    5528 B |        7.05 |
