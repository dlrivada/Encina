```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,498.6 ns |  1,849.91 ns | 1,639.90 ns |  6,982.0 ns |  1.07 |    0.41 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,613.1 ns |    874.47 ns |   730.22 ns |  5,470.0 ns |  0.93 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    243.9 ns |     30.10 ns |    26.68 ns |    236.5 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,039.5 ns |    296.39 ns |   247.50 ns |  6,997.5 ns |  1.16 |    0.33 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  9,368.3 ns |    389.80 ns |   325.50 ns |  9,315.5 ns |  1.54 |    0.44 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,273.8 ns | 64,206.66 ns | 3,519.38 ns |  6,721.5 ns |  1.11 |    0.54 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,077.2 ns | 63,487.56 ns | 3,479.97 ns |  7,601.5 ns |  1.08 |    0.54 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    471.0 ns |  5,850.04 ns |   320.66 ns |    313.0 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,616.2 ns | 64,792.97 ns | 3,551.52 ns |  6,362.5 ns |  1.02 |    0.53 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 17,542.8 ns | 50,278.40 ns | 2,755.93 ns | 16,361.5 ns |  2.35 |    0.79 |    5528 B |        7.05 |
