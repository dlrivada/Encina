```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error         | StdDev      | Median        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-------------:|--------------:|------------:|--------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  2,662.00 ns |   1,120.30 ns |   993.11 ns | 2,189.5000 ns |  1.12 |    0.54 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  3,745.15 ns |   1,639.93 ns | 1,369.42 ns | 3,360.0000 ns |  1.57 |    0.75 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |     44.86 ns |      73.71 ns |    65.34 ns |     0.0000 ns |  0.02 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  2,602.31 ns |     960.27 ns |   801.87 ns | 2,179.0000 ns |  1.09 |    0.47 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  7,292.79 ns |   1,321.98 ns | 1,171.90 ns | 7,351.0000 ns |  3.06 |    1.05 |    5528 B |        7.05 |
|                                |            |                |             |             |              |               |             |               |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,577.33 ns | 109,235.39 ns | 5,987.56 ns | 4,371.0000 ns |  1.76 |    2.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 11,608.33 ns | 118,795.69 ns | 6,511.59 ns | 9,998.0000 ns |  3.10 |    2.81 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    380.67 ns |   4,187.41 ns |   229.53 ns |   441.0000 ns |  0.10 |    0.10 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,413.17 ns |  81,273.70 ns | 4,454.89 ns | 3,987.5000 ns |  1.71 |    1.71 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           |  9,753.00 ns |  90,859.30 ns | 4,980.30 ns | 7,999.0000 ns |  2.61 |    2.28 |    5528 B |        7.05 |
