```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,515.6 ns |     238.24 ns |    211.19 ns |  5,438.5 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  4,691.1 ns |     389.96 ns |    345.69 ns |  4,637.5 ns |  0.85 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    329.6 ns |      83.27 ns |     73.81 ns |    330.5 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,504.4 ns |     333.01 ns |    295.21 ns |  5,492.5 ns |  1.00 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 11,061.5 ns |   1,558.17 ns |  1,301.14 ns | 11,427.0 ns |  2.01 |    0.24 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,959.0 ns |   9,931.88 ns |    544.40 ns |  5,849.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,779.0 ns |   6,109.90 ns |    334.90 ns |  5,688.0 ns |  0.98 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  8,392.7 ns | 221,078.12 ns | 12,118.04 ns |  2,223.0 ns |  1.42 |    1.78 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,705.2 ns |   8,297.27 ns |    454.80 ns |  5,468.5 ns |  0.96 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           |  8,735.7 ns |  15,820.80 ns |    867.19 ns |  9,133.0 ns |  1.47 |    0.17 |    5528 B |        7.05 |
