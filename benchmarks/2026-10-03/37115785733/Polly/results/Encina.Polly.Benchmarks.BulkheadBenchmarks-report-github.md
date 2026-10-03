```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 3,351.00 ns |     513.12 ns |   454.87 ns | 3,244.50 ns |  1.02 |    0.18 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 1,547.08 ns |     447.81 ns |   373.94 ns | 1,352.00 ns |  0.47 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    44.31 ns |      28.79 ns |    24.04 ns |    35.00 ns |  0.01 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 1,311.17 ns |     434.23 ns |   339.02 ns | 1,162.00 ns |  0.40 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 6,501.00 ns |     577.24 ns |   482.02 ns | 6,444.00 ns |  1.97 |    0.28 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 3,675.67 ns |  39,496.08 ns | 2,164.91 ns | 2,955.00 ns |  1.24 |    0.88 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 4,169.33 ns |  35,583.12 ns | 1,950.43 ns | 3,825.00 ns |  1.41 |    0.88 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   210.33 ns |   4,468.05 ns |   244.91 ns |   100.00 ns |  0.07 |    0.08 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 3,777.33 ns |  32,518.61 ns | 1,782.45 ns | 3,130.00 ns |  1.27 |    0.80 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,119.00 ns | 112,060.90 ns | 6,142.44 ns | 8,277.00 ns |  3.07 |    2.38 |    5528 B |        7.05 |
