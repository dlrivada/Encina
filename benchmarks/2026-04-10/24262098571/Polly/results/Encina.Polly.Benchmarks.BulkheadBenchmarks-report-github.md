```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     |  6,978.5 ns |   908.2 ns | 140.55 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     |  6,865.5 ns | 1,923.0 ns | 297.59 ns |  0.98 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     |    538.8 ns |   122.7 ns |  18.98 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     |  6,792.8 ns |   650.3 ns | 100.63 ns |  0.97 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 10,909.8 ns | 1,284.1 ns | 333.49 ns |  1.56 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |            |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           |  7,037.0 ns | 3,202.1 ns | 175.52 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           |  7,075.3 ns | 3,300.1 ns | 180.89 ns |  1.01 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           |    581.2 ns |   493.0 ns |  27.02 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           |  7,076.0 ns | 5,039.3 ns | 276.22 ns |  1.01 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 11,076.7 ns | 4,313.6 ns | 236.44 ns |  1.57 |    0.04 |    5528 B |        7.05 |
