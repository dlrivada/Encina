```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|---------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,729.0 ns |    491.4 ns | 435.6 ns | 5,592.5 ns |  1.01 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,037.2 ns |    320.7 ns | 284.3 ns | 4,997.5 ns |  0.88 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   405.2 ns |    130.8 ns | 109.2 ns |   380.0 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,231.9 ns |    321.3 ns | 284.8 ns | 5,177.5 ns |  0.92 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,155.9 ns |    286.4 ns | 239.2 ns | 8,121.0 ns |  1.43 |    0.11 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |          |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,673.7 ns | 14,259.3 ns | 781.6 ns | 5,263.0 ns |  1.01 |    0.17 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,891.3 ns |  9,774.4 ns | 535.8 ns | 5,688.0 ns |  1.05 |    0.14 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   364.0 ns |  3,755.2 ns | 205.8 ns |   261.0 ns |  0.06 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,799.0 ns | 15,363.9 ns | 842.1 ns | 5,338.0 ns |  1.03 |    0.17 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,705.0 ns | 14,907.0 ns | 817.1 ns | 8,347.0 ns |  1.55 |    0.21 |    5528 B |        7.05 |
