```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,116.3 ns |     832.1 ns |    694.80 ns |  8,206.0 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,990.5 ns |     128.9 ns |    107.62 ns |  6,978.5 ns |  0.87 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    776.7 ns |     264.7 ns |    234.67 ns |    876.0 ns |  0.10 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,407.8 ns |     707.5 ns |    661.79 ns |  7,017.5 ns |  0.92 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,858.1 ns |     143.3 ns |    119.67 ns | 10,835.5 ns |  1.35 |    0.11 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  9,315.8 ns |  33,928.4 ns |  1,859.73 ns |  8,651.5 ns |  1.03 |    0.24 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 11,047.5 ns |  39,893.5 ns |  2,186.70 ns | 10,650.5 ns |  1.22 |    0.29 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    629.0 ns |   1,692.5 ns |     92.77 ns |    655.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  8,146.5 ns |  20,776.5 ns |  1,138.83 ns |  7,618.5 ns |  0.90 |    0.18 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 21,620.8 ns | 206,890.0 ns | 11,340.34 ns | 15,979.5 ns |  2.38 |    1.16 |    5528 B |        7.05 |
