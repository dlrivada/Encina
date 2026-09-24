```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,047.8 ns |   256.13 ns | 213.88 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,897.5 ns |   145.57 ns | 113.65 ns |  0.98 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    571.9 ns |    28.80 ns |  22.49 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,777.6 ns |   127.75 ns | 106.68 ns |  0.96 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,794.1 ns |   204.19 ns | 159.42 ns |  1.53 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,353.7 ns | 3,339.66 ns | 183.06 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,323.2 ns | 4,178.98 ns | 229.06 ns |  1.00 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    579.8 ns |   210.66 ns |  11.55 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,049.3 ns | 4,628.31 ns | 253.69 ns |  0.96 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,181.0 ns | 2,986.63 ns | 163.71 ns |  1.52 |    0.04 |    5528 B |        7.05 |
