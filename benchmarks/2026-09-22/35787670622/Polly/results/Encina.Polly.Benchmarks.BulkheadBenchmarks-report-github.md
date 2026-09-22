```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  3,739.0 ns |    851.20 ns |   664.56 ns |  3,439.0 ns |  1.02 |    0.23 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,093.0 ns |  1,347.94 ns | 1,194.92 ns |  4,554.5 ns |  1.39 |    0.37 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    287.4 ns |     99.12 ns |    87.87 ns |    270.0 ns |  0.08 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,531.6 ns |  2,453.01 ns | 2,294.55 ns |  4,211.5 ns |  1.51 |    0.65 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  7,769.7 ns |    417.27 ns |   348.44 ns |  7,816.5 ns |  2.13 |    0.31 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,772.0 ns | 61,419.67 ns | 3,366.62 ns |  5,591.0 ns |  1.16 |    0.68 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,071.0 ns | 55,598.69 ns | 3,047.55 ns |  6,385.0 ns |  1.38 |    0.70 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    442.3 ns |  5,436.88 ns |   298.01 ns |    307.0 ns |  0.08 |    0.05 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,825.3 ns | 51,004.24 ns | 2,795.71 ns |  7,914.0 ns |  1.51 |    0.71 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,256.2 ns | 57,887.13 ns | 3,172.99 ns | 12,320.5 ns |  2.10 |    0.92 |    5528 B |        7.05 |
