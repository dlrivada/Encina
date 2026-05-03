```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,908.4 ns |   141.90 ns |   125.79 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,956.2 ns |   101.89 ns |    90.32 ns |  1.01 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    608.4 ns |    30.57 ns |    27.10 ns |  0.09 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,910.1 ns |   161.06 ns |   134.49 ns |  1.00 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 13,531.7 ns | 2,217.49 ns | 1,965.75 ns |  1.96 |    0.28 |    5528 B |        7.05 |
|                                |            |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  7,386.1 ns |   359.38 ns |   515.41 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,517.4 ns |   498.75 ns |   715.29 ns |  1.02 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    560.0 ns |    34.94 ns |    51.21 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  7,296.5 ns |   395.36 ns |   554.24 ns |  0.99 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,585.9 ns |   147.50 ns |   196.90 ns |  1.44 |    0.10 |    5528 B |        7.05 |
