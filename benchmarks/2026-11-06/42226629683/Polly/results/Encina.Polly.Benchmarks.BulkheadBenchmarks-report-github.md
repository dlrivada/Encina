```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,236.0 ns |     409.8 ns |   342.24 ns |  7,103.0 ns |  1.00 |    0.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,203.8 ns |     386.9 ns |   342.99 ns |  7,033.0 ns |  1.00 |    0.06 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    649.3 ns |     269.1 ns |   251.68 ns |    617.0 ns |  0.09 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,171.9 ns |     969.8 ns |   859.70 ns |  6,748.0 ns |  0.99 |    0.12 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 14,801.6 ns |   3,238.0 ns | 2,703.88 ns | 14,757.5 ns |  2.05 |    0.37 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,858.3 ns |  13,278.3 ns |   727.83 ns |  8,196.0 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,839.8 ns |  12,514.0 ns |   685.93 ns |  9,136.5 ns |  1.13 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    504.7 ns |   1,391.8 ns |    76.29 ns |    471.0 ns |  0.06 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 13,101.0 ns | 165,969.9 ns | 9,097.37 ns |  8,015.0 ns |  1.68 |    1.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,042.7 ns |  12,880.3 ns |   706.01 ns | 12,233.0 ns |  1.54 |    0.15 |    5528 B |        7.05 |
