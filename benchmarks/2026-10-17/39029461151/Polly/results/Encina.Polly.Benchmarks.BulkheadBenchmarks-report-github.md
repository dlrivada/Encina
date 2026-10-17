```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,643.7 ns |  1,888.29 ns | 1,576.81 ns | 5,210.5 ns |  1.07 |    0.40 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,322.2 ns |  2,793.42 ns | 2,476.29 ns | 5,655.5 ns |  1.20 |    0.56 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    182.8 ns |     27.49 ns |    22.96 ns |   189.0 ns |  0.03 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,551.2 ns |    187.28 ns |   146.21 ns | 6,525.0 ns |  1.24 |    0.31 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,536.9 ns |    641.95 ns |   501.19 ns | 8,431.0 ns |  1.62 |    0.42 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  9,232.0 ns | 55,017.05 ns | 3,015.67 ns | 8,566.0 ns |  1.07 |    0.42 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,052.7 ns | 52,974.56 ns | 2,903.71 ns | 5,951.0 ns |  0.82 |    0.37 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    507.2 ns |  3,695.39 ns |   202.56 ns |   404.5 ns |  0.06 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,243.3 ns | 42,381.53 ns | 2,323.07 ns | 6,476.0 ns |  0.84 |    0.33 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,289.0 ns | 61,330.96 ns | 3,361.76 ns | 9,904.0 ns |  1.31 |    0.49 |    5528 B |        7.05 |
