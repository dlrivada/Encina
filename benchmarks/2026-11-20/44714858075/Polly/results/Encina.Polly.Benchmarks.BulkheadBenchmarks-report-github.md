```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  2,519.9 ns |     896.4 ns |    794.7 ns | 2,062.00 ns |  1.08 |    0.43 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  3,212.9 ns |   1,296.6 ns |  1,149.4 ns | 2,835.00 ns |  1.38 |    0.59 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    111.1 ns |     113.9 ns |    101.0 ns |    88.00 ns |  0.05 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  2,465.2 ns |   1,005.8 ns |    891.6 ns | 1,990.50 ns |  1.06 |    0.46 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  7,532.4 ns |     739.3 ns |    655.3 ns | 7,224.00 ns |  3.22 |    0.83 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,751.3 ns | 109,321.0 ns |  5,992.3 ns | 4,491.00 ns |  1.68 |    1.88 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,497.8 ns |  59,134.5 ns |  3,241.4 ns | 4,946.50 ns |  1.37 |    1.22 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    263.7 ns |   7,963.2 ns |    436.5 ns |    23.50 ns |  0.07 |    0.12 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,554.7 ns |  77,510.7 ns |  4,248.6 ns | 6,517.00 ns |  1.63 |    1.53 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 16,407.0 ns | 306,878.9 ns | 16,821.1 ns | 7,905.00 ns |  4.09 |    5.05 |    5528 B |        7.05 |
