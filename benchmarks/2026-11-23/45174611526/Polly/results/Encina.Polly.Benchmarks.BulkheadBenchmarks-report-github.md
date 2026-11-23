```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,733.6 ns |     892.95 ns |    791.58 ns |  7,690.0 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,089.8 ns |     332.53 ns |    277.68 ns |  7,013.0 ns |  0.93 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    535.5 ns |      63.21 ns |     56.04 ns |    530.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  8,376.6 ns |   2,074.79 ns |  1,839.25 ns |  7,829.5 ns |  1.09 |    0.25 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,831.5 ns |     336.46 ns |    262.68 ns | 10,776.0 ns |  1.41 |    0.14 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 14,380.3 ns | 220,765.22 ns | 12,100.89 ns |  7,464.0 ns |  1.47 |    1.38 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,087.0 ns |   5,067.91 ns |    277.79 ns |  7,014.0 ns |  0.72 |    0.36 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    523.7 ns |     893.82 ns |     48.99 ns |    500.0 ns |  0.05 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,215.5 ns |  12,762.67 ns |    699.56 ns |  7,905.5 ns |  0.84 |    0.42 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 15,567.0 ns |  64,596.98 ns |  3,540.78 ns | 14,822.0 ns |  1.59 |    0.85 |    5528 B |        7.05 |
