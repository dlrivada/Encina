```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev     | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,242.6 ns |  1,090.9 ns |   967.0 ns |  6,401.5 ns |  1.03 |    0.25 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,904.7 ns |  1,515.8 ns | 1,265.7 ns |  7,352.5 ns |  1.14 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    212.8 ns |    127.4 ns |   106.4 ns |    166.0 ns |  0.04 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  3,790.1 ns |    278.1 ns |   232.2 ns |  3,886.0 ns |  0.62 |    0.12 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  9,791.8 ns |  1,084.9 ns |   905.9 ns |  9,736.5 ns |  1.61 |    0.33 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,422.3 ns | 84,029.6 ns | 4,605.9 ns |  7,553.0 ns |  1.23 |    0.86 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,337.3 ns | 72,337.3 ns | 3,965.1 ns |  6,778.0 ns |  1.22 |    0.79 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    546.5 ns |  6,337.2 ns |   347.4 ns |    382.5 ns |  0.08 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,147.7 ns | 39,419.2 ns | 2,160.7 ns |  7,114.0 ns |  1.05 |    0.58 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,334.0 ns | 60,681.3 ns | 3,326.1 ns | 11,712.0 ns |  1.66 |    0.91 |    5528 B |        7.05 |
