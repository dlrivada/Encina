```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  1,399.6 ns |     233.9 ns |   195.3 ns | 1,378.0 ns |  1.02 |    0.19 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  3,816.9 ns |   2,231.3 ns | 2,087.1 ns | 2,890.0 ns |  2.77 |    1.52 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    193.1 ns |     199.6 ns |   186.7 ns |   110.0 ns |  0.14 |    0.13 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  3,619.4 ns |     719.6 ns |   600.9 ns | 3,495.5 ns |  2.63 |    0.54 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  3,661.5 ns |     447.9 ns |   349.7 ns | 3,580.5 ns |  2.66 |    0.42 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |            |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,455.0 ns |  38,565.0 ns | 2,113.9 ns | 4,747.0 ns |  1.10 |    0.50 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  5,824.0 ns |  27,082.2 ns | 1,484.5 ns | 6,254.0 ns |  1.17 |    0.44 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  1,629.3 ns |   5,915.6 ns |   324.3 ns | 1,463.0 ns |  0.33 |    0.11 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  5,439.2 ns |  39,516.9 ns | 2,166.1 ns | 5,041.5 ns |  1.09 |    0.51 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 10,642.3 ns | 127,658.0 ns | 6,997.4 ns | 6,830.0 ns |  2.14 |    1.41 |    5528 B |        7.05 |
