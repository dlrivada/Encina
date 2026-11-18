```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| RecordFailure                       | Job-IAMMPO | 15             | Default     | 5           |   165.6 ns |     36.70 ns |    30.64 ns |   175.5 ns |  0.04 |    0.01 |         - |        0.00 |
| GetState                            | Job-IAMMPO | 15             | Default     | 5           | 1,623.8 ns |  1,098.21 ns | 1,027.26 ns | 1,758.5 ns |  0.36 |    0.24 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,678.3 ns |    472.53 ns |   418.89 ns | 4,655.5 ns |  1.03 |    0.27 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | Job-IAMMPO | 15             | Default     | 5           | 4,869.5 ns |    731.46 ns |   648.42 ns | 4,827.5 ns |  1.07 |    0.30 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | Job-IAMMPO | 15             | Default     | 5           | 5,087.6 ns |  1,076.17 ns |   953.99 ns | 5,124.5 ns |  1.12 |    0.35 |     440 B |        1.00 |
| RecordSuccess                       | Job-IAMMPO | 15             | Default     | 5           |   220.3 ns |     48.88 ns |    40.82 ns |   235.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | Job-IAMMPO | 15             | Default     | 5           | 4,709.1 ns |    901.90 ns |   753.13 ns | 4,932.0 ns |  1.04 |    0.30 |     440 B |        1.00 |
|                                     |            |                |             |             |            |              |             |            |       |         |           |             |
| RecordFailure                       | ShortRun   | 3              | 1           | 3           |   423.7 ns |  5,886.37 ns |   322.65 ns |   288.0 ns |  0.14 |    0.11 |         - |        0.00 |
| GetState                            | ShortRun   | 3              | 1           | 3           |   349.3 ns |  5,116.10 ns |   280.43 ns |   228.0 ns |  0.12 |    0.09 |         - |        0.00 |
| AcquireAndRecordSuccess_Combined    | ShortRun   | 3              | 1           | 3           | 3,859.5 ns | 26,276.61 ns | 1,440.31 ns | 3,493.5 ns |  1.32 |    0.59 |     336 B |        0.76 |
| AcquireAndRecordFailure_Combined    | ShortRun   | 3              | 1           | 3           | 5,095.0 ns | 42,783.79 ns | 2,345.12 ns | 4,128.0 ns |  1.74 |    0.88 |     336 B |        0.76 |
| AcquireAsync_WithAdaptiveThrottling | ShortRun   | 3              | 1           | 3           | 4,662.2 ns | 49,436.09 ns | 2,709.76 ns | 3,892.5 ns |  1.59 |    0.95 |     440 B |        1.00 |
| RecordSuccess                       | ShortRun   | 3              | 1           | 3           | 2,133.8 ns | 27,500.63 ns | 1,507.40 ns | 2,584.5 ns |  0.73 |    0.51 |         - |        0.00 |
| AcquireAsync_SimpleRateLimiting     | ShortRun   | 3              | 1           | 3           | 3,246.7 ns | 24,987.81 ns | 1,369.67 ns | 2,558.0 ns |  1.11 |    0.53 |     440 B |        1.00 |
