```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,201.7 ns |     694.93 ns |   616.04 ns | 5,173.0 ns |  1.01 |    0.16 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,100.7 ns |     296.90 ns |   263.19 ns | 4,988.0 ns |  0.99 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   337.2 ns |      30.24 ns |    26.81 ns |   336.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,231.6 ns |     518.54 ns |   433.01 ns | 5,137.5 ns |  1.02 |    0.14 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,024.6 ns |     325.32 ns |   288.39 ns | 7,922.5 ns |  1.56 |    0.18 |    5528 B |        7.05 |
|                                |            |                |             |             |            |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,662.0 ns |  11,835.49 ns |   648.74 ns | 5,488.0 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 8,005.7 ns |  83,498.52 ns | 4,576.84 ns | 5,429.0 ns |  1.43 |    0.72 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   466.0 ns |   4,274.64 ns |   234.31 ns |   346.0 ns |  0.08 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 9,291.0 ns | 109,427.67 ns | 5,998.10 ns | 6,420.0 ns |  1.65 |    0.94 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,637.0 ns |  10,894.54 ns |   597.17 ns | 8,333.0 ns |  1.54 |    0.17 |    5528 B |        7.05 |
