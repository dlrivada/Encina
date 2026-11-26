```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,089.8 ns |  4,013.11 ns | 3,133.17 ns |  1.07 |    0.45 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,806.4 ns |    269.73 ns |   239.11 ns |  0.90 |    0.17 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    523.3 ns |     59.45 ns |    49.64 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,903.6 ns |    285.82 ns |   223.15 ns |  0.91 |    0.17 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,733.1 ns |    314.12 ns |   245.24 ns |  1.42 |    0.26 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  9,897.0 ns | 22,875.69 ns | 1,253.89 ns |  1.01 |    0.16 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,705.8 ns |  7,236.35 ns |   396.65 ns |  0.79 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    601.0 ns |  2,849.76 ns |   156.20 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,391.7 ns | 12,598.49 ns |   690.57 ns |  0.76 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,096.7 ns | 15,949.15 ns |   874.23 ns |  1.13 |    0.15 |    5528 B |        7.05 |
