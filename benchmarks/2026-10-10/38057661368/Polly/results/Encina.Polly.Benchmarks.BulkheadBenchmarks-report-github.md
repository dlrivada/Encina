```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,043.8 ns |    475.42 ns |   397.00 ns |  6,990.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,058.8 ns |    508.83 ns |   424.90 ns |  7,006.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    272.7 ns |     50.51 ns |    42.18 ns |    259.5 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,699.9 ns |  1,458.67 ns | 1,293.08 ns |  5,098.0 ns |  0.81 |    0.18 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  9,487.2 ns |    424.85 ns |   331.69 ns |  9,419.5 ns |  1.35 |    0.08 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 10,572.5 ns | 70,155.89 ns | 3,845.48 ns |  9,014.5 ns |  1.08 |    0.46 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  9,160.0 ns | 47,425.97 ns | 2,599.58 ns |  8,052.0 ns |  0.94 |    0.35 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    520.8 ns |  6,035.40 ns |   330.82 ns |    367.5 ns |  0.05 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,622.7 ns | 60,263.62 ns | 3,303.25 ns |  6,390.0 ns |  0.78 |    0.37 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,215.3 ns | 82,210.66 ns | 4,506.24 ns | 11,621.0 ns |  1.35 |    0.55 |    5528 B |        7.05 |
