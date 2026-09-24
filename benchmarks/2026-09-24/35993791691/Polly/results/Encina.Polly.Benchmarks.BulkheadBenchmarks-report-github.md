```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,541.0 ns |    601.0 ns | 562.13 ns |  1.01 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,983.1 ns |    489.2 ns | 408.49 ns |  0.93 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    603.5 ns |    114.9 ns | 101.84 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,442.0 ns |    741.3 ns | 657.17 ns |  0.99 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,151.0 ns |    493.5 ns | 461.61 ns |  1.49 |    0.12 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,260.3 ns | 12,019.6 ns | 658.83 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,677.0 ns |  5,786.4 ns | 317.17 ns |  0.93 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    587.5 ns |    686.7 ns |  37.64 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,290.3 ns |  8,629.1 ns | 472.99 ns |  0.89 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,976.7 ns | 17,478.5 ns | 958.06 ns |  1.70 |    0.15 |    5528 B |        7.05 |
