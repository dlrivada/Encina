```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,936.0 ns |    147.12 ns |   130.42 ns |  6,917.0 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  8,120.2 ns |    262.14 ns |   204.66 ns |  8,085.5 ns |  1.17 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    518.0 ns |     29.55 ns |    26.20 ns |    516.5 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,997.9 ns |    347.00 ns |   307.61 ns |  6,893.0 ns |  1.01 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,237.3 ns |  2,602.78 ns | 2,434.65 ns | 15,540.0 ns |  2.05 |    0.34 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,240.0 ns |  4,748.52 ns |   260.28 ns |  7,254.0 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,537.7 ns | 80,055.51 ns | 4,388.11 ns |  8,410.0 ns |  1.46 |    0.53 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    570.7 ns |    621.45 ns |    34.06 ns |    551.0 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,093.2 ns |  4,347.61 ns |   238.31 ns |  7,002.5 ns |  0.98 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,255.0 ns |  7,209.11 ns |   395.16 ns | 11,321.0 ns |  1.56 |    0.07 |    5528 B |        7.05 |
