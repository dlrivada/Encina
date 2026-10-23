```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,072.4 ns |     326.24 ns |   289.20 ns |  6,958.5 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,043.0 ns |     187.43 ns |   166.15 ns |  7,003.5 ns |  1.00 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    564.6 ns |      63.59 ns |    49.65 ns |    570.0 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,934.8 ns |     119.70 ns |    99.95 ns |  6,907.5 ns |  0.98 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,972.7 ns |     358.79 ns |   299.61 ns | 10,905.5 ns |  1.55 |    0.07 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 11,208.0 ns | 123,250.93 ns | 6,755.80 ns |  7,494.0 ns |  1.22 |    0.83 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,294.3 ns |   5,577.04 ns |   305.70 ns |  7,154.0 ns |  0.79 |    0.31 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    551.7 ns |     791.10 ns |    43.36 ns |    571.0 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,964.8 ns |   5,037.39 ns |   276.12 ns |  6,857.5 ns |  0.76 |    0.30 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,812.3 ns |  48,728.82 ns | 2,670.99 ns | 11,517.0 ns |  1.39 |    0.60 |    5528 B |        7.05 |
