```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,679.1 ns |     648.91 ns |   575.24 ns |  7,818.5 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  4,806.1 ns |     511.79 ns |   427.37 ns |  4,667.5 ns |  0.63 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    212.1 ns |      25.89 ns |    22.95 ns |    213.0 ns |  0.03 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,804.0 ns |     201.90 ns |   157.63 ns |  4,816.0 ns |  0.63 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,224.1 ns |     789.68 ns |   659.42 ns | 10,972.5 ns |  1.47 |    0.14 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,917.7 ns |  60,866.15 ns | 3,336.28 ns |  6,931.0 ns |  1.12 |    0.56 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 12,646.5 ns | 126,368.41 ns | 6,926.68 ns | 12,534.5 ns |  1.78 |    1.06 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    530.5 ns |   7,256.16 ns |   397.73 ns |    314.5 ns |  0.07 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,747.2 ns |  60,503.12 ns | 3,316.38 ns |  7,368.5 ns |  1.23 |    0.59 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 14,410.7 ns |  78,327.94 ns | 4,293.42 ns | 12,932.0 ns |  2.03 |    0.86 |    5528 B |        7.05 |
