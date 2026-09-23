```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,180.3 ns |    361.3 ns |   301.72 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  8,205.0 ns |    972.9 ns |   862.43 ns |  1.14 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    606.8 ns |    100.5 ns |    89.11 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,178.0 ns |    373.3 ns |   330.96 ns |  1.00 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 12,857.4 ns |  2,723.8 ns | 2,414.62 ns |  1.79 |    0.33 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,649.5 ns | 12,875.8 ns |   705.77 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,981.0 ns |  4,235.0 ns |   232.14 ns |  0.93 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    598.0 ns |  1,337.8 ns |    73.33 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 10,039.0 ns | 56,282.7 ns | 3,085.05 ns |  1.17 |    0.32 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,637.3 ns |  9,973.5 ns |   546.68 ns |  1.35 |    0.11 |    5528 B |        7.05 |
