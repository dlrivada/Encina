```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-------------:|--------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  3,195.00 ns |     293.81 ns |    245.35 ns | 3,105.00 ns |  1.01 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  2,025.54 ns |     608.17 ns |    507.85 ns | 1,943.00 ns |  0.64 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |     38.69 ns |      27.87 ns |     23.28 ns |    40.00 ns |  0.01 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  1,819.69 ns |     283.45 ns |    236.69 ns | 1,763.00 ns |  0.57 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  3,583.96 ns |     314.06 ns |    262.25 ns | 3,520.50 ns |  1.13 |    0.11 |    5528 B |        7.05 |
|                                |            |                |             |             |              |               |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,218.33 ns |  37,447.36 ns |  2,052.62 ns | 4,788.00 ns |  1.11 |    0.53 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  4,917.67 ns |  26,181.59 ns |  1,435.10 ns | 5,088.00 ns |  1.04 |    0.44 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    481.00 ns |   4,939.30 ns |    270.74 ns |   361.00 ns |  0.10 |    0.06 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  4,113.33 ns |  33,826.29 ns |  1,854.13 ns | 3,526.00 ns |  0.87 |    0.45 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,432.33 ns | 239,377.29 ns | 13,121.08 ns | 5,368.00 ns |  2.64 |    2.66 |    5528 B |        7.05 |
