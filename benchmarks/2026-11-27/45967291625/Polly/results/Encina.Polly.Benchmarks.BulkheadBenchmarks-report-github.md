```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,313.1 ns |     170.8 ns |    133.3 ns |  5,297.5 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,201.6 ns |     234.7 ns |    208.1 ns |  5,208.0 ns |  0.98 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    386.1 ns |     135.2 ns |    126.5 ns |    415.5 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,287.5 ns |     245.6 ns |    205.1 ns |  5,358.0 ns |  1.00 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,519.2 ns |     256.6 ns |    214.3 ns |  8,513.0 ns |  1.60 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,927.2 ns |   9,140.8 ns |    501.0 ns |  5,934.5 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,912.3 ns |  14,173.7 ns |    776.9 ns |  5,869.0 ns |  1.00 |    0.14 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    367.7 ns |   2,620.6 ns |    143.6 ns |    311.0 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,017.7 ns |  15,954.9 ns |    874.5 ns |  5,703.0 ns |  1.02 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 15,440.0 ns | 201,424.9 ns | 11,040.8 ns | 10,135.0 ns |  2.62 |    1.64 |    5528 B |        7.05 |
