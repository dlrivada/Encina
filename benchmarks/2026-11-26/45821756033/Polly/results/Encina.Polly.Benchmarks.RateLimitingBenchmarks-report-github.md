```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   229.4 ns |     52.40 ns |    46.46 ns |   217.5 ns |  0.05 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           |   227.1 ns |     50.02 ns |    44.34 ns |   217.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 3,942.2 ns |    197.92 ns |   165.27 ns | 3,973.5 ns |  0.89 |    0.06 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 2,764.5 ns |  1,056.64 ns |   936.69 ns | 2,248.5 ns |  0.62 |    0.21 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 4,386.9 ns |    237.04 ns |   197.94 ns | 4,326.5 ns |  0.99 |    0.06 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   221.6 ns |     29.97 ns |    26.57 ns |   213.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,455.3 ns |    268.74 ns |   238.23 ns | 4,392.0 ns |  1.00 |    0.07 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   428.5 ns |  4,804.34 ns |   263.34 ns |   336.5 ns |  0.14 |    0.09 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           | 1,555.3 ns | 12,270.34 ns |   672.58 ns | 1,925.0 ns |  0.50 |    0.25 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,779.0 ns | 27,130.03 ns | 1,487.09 ns | 3,038.0 ns |  1.23 |    0.57 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 3,974.0 ns | 18,100.12 ns |   992.13 ns | 3,759.0 ns |  1.29 |    0.49 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 5,694.2 ns | 97,292.68 ns | 5,332.94 ns | 2,933.5 ns |  1.85 |    1.66 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           |   501.0 ns |  6,471.79 ns |   354.74 ns |   316.0 ns |  0.16 |    0.11 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,421.8 ns | 26,748.14 ns | 1,466.16 ns | 2,733.5 ns |  1.11 |    0.55 |     440 B |        1.00 |
