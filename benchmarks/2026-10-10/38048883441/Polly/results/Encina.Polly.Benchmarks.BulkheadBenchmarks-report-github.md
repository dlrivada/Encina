```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,711.8 ns |    575.85 ns |   480.86 ns |  5,607.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,063.0 ns |    890.74 ns |   743.81 ns |  6,129.5 ns |  1.07 |    0.15 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    281.2 ns |     58.93 ns |    49.21 ns |    285.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  4,524.1 ns |    262.89 ns |   219.53 ns |  4,498.5 ns |  0.80 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,534.8 ns |  1,023.60 ns |   854.75 ns | 14,676.0 ns |  2.56 |    0.25 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,473.5 ns | 87,162.95 ns | 4,777.70 ns |  6,792.5 ns |  1.21 |    0.81 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,571.5 ns | 82,190.16 ns | 4,505.12 ns |  6,533.5 ns |  1.23 |    0.79 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  1,252.2 ns | 30,001.88 ns | 1,644.50 ns |    370.5 ns |  0.18 |    0.23 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,693.2 ns | 74,053.96 ns | 4,059.15 ns |  7,346.5 ns |  1.24 |    0.75 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,214.8 ns | 70,450.90 ns | 3,861.65 ns | 11,981.5 ns |  1.89 |    0.94 |    5528 B |        7.05 |
