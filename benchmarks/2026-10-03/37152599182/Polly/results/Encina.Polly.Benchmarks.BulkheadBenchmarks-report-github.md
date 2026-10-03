```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,169.9 ns |     426.17 ns |   377.79 ns |  7,120.5 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,096.9 ns |     792.00 ns |   740.84 ns |  7,101.0 ns |  0.99 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    483.1 ns |      67.91 ns |    63.52 ns |    462.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,548.7 ns |     269.31 ns |   238.74 ns |  6,505.0 ns |  0.92 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,746.4 ns |     605.86 ns |   566.73 ns | 10,645.5 ns |  1.50 |    0.11 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 13,960.8 ns | 169,533.16 ns | 9,292.68 ns |  9,223.5 ns |  1.28 |    0.96 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,826.2 ns |  20,711.36 ns | 1,135.26 ns |  7,535.5 ns |  0.72 |    0.32 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    694.2 ns |   6,140.86 ns |   336.60 ns |    620.5 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,684.3 ns |  18,195.83 ns |   997.37 ns |  7,400.0 ns |  0.70 |    0.31 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,824.7 ns |  21,670.55 ns | 1,187.84 ns | 14,322.0 ns |  1.26 |    0.55 |    5528 B |        7.05 |
