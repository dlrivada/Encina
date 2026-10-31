```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           | 1,803.0 ns |    257.14 ns |   240.53 ns | 1,849.5 ns |  0.81 |    0.12 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   395.5 ns |    151.60 ns |   126.59 ns |   377.0 ns |  0.18 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,113.3 ns |    145.90 ns |   113.91 ns | 2,080.5 ns |  0.95 |    0.08 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 1,940.7 ns |    151.40 ns |   118.20 ns | 1,899.5 ns |  0.87 |    0.08 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 3,616.4 ns |  1,247.04 ns | 1,166.48 ns | 3,112.5 ns |  1.62 |    0.52 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   227.9 ns |     34.77 ns |    29.04 ns |   224.0 ns |  0.10 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 2,239.0 ns |    191.67 ns |   160.05 ns | 2,194.0 ns |  1.00 |    0.10 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   468.8 ns |  5,624.11 ns |   308.28 ns |   346.5 ns |  0.08 |    0.05 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   594.8 ns |  7,144.87 ns |   391.63 ns |   419.5 ns |  0.10 |    0.06 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 4,102.0 ns | 34,254.15 ns | 1,877.59 ns | 3,706.0 ns |  0.72 |    0.32 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,737.2 ns | 36,187.22 ns | 1,983.54 ns | 2,921.5 ns |  0.66 |    0.33 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 3,912.8 ns | 39,506.67 ns | 2,165.49 ns | 2,905.5 ns |  0.69 |    0.36 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   371.2 ns |  5,947.60 ns |   326.01 ns |   208.5 ns |  0.07 |    0.05 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 5,903.0 ns | 25,125.01 ns | 1,377.19 ns | 5,517.0 ns |  1.03 |    0.29 |     440 B |        1.00 |
