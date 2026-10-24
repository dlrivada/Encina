```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   466.2 ns |     29.01 ns |    24.23 ns |   470.5 ns |  0.12 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   515.6 ns |     59.77 ns |    52.98 ns |   511.5 ns |  0.13 |    0.02 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,488.5 ns |    638.35 ns |   597.11 ns | 4,238.0 ns |  1.11 |    0.16 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,676.1 ns |    363.16 ns |   321.93 ns | 4,583.0 ns |  1.16 |    0.11 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,558.6 ns |    331.39 ns |   293.77 ns | 3,456.0 ns |  0.88 |    0.10 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   474.1 ns |     33.65 ns |    29.83 ns |   476.5 ns |  0.12 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,059.2 ns |    324.40 ns |   303.44 ns | 4,063.5 ns |  1.01 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           | 2,755.3 ns | 41,310.82 ns | 2,264.39 ns | 3,778.0 ns |  0.30 |    0.21 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   655.3 ns |    822.66 ns |    45.09 ns |   652.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,950.0 ns |  7,342.98 ns |   402.49 ns | 4,910.0 ns |  0.53 |    0.05 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 4,518.7 ns | 12,774.01 ns |   700.19 ns | 4,308.0 ns |  0.48 |    0.07 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,061.7 ns |  4,299.50 ns |   235.67 ns | 4,072.0 ns |  0.44 |    0.03 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   557.0 ns |  3,400.73 ns |   186.41 ns |   470.0 ns |  0.06 |    0.02 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 9,359.3 ns | 11,474.02 ns |   628.93 ns | 9,503.0 ns |  1.00 |    0.08 |     440 B |        1.00 |
