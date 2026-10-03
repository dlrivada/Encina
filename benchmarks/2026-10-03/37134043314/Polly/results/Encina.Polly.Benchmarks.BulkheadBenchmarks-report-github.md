```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,872.1 ns |    544.95 ns |   455.06 ns |  5,972.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,749.0 ns |    704.03 ns |   587.90 ns |  5,779.5 ns |  0.99 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    213.4 ns |     22.79 ns |    19.03 ns |    212.5 ns |  0.04 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  3,776.3 ns |    248.55 ns |   207.55 ns |  3,752.5 ns |  0.65 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,236.4 ns |    392.36 ns |   327.63 ns |  8,212.0 ns |  1.41 |    0.13 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,259.8 ns | 68,427.05 ns | 3,750.72 ns |  6,040.5 ns |  1.18 |    0.73 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 10,790.8 ns | 92,269.79 ns | 5,057.62 ns | 10,699.5 ns |  1.75 |    1.02 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  1,131.3 ns | 28,231.95 ns | 1,547.49 ns |    370.0 ns |  0.18 |    0.24 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,887.2 ns | 50,709.44 ns | 2,779.56 ns |  6,872.5 ns |  1.28 |    0.65 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 13,823.2 ns | 57,469.48 ns | 3,150.10 ns | 15,295.5 ns |  2.24 |    1.00 |    5528 B |        7.05 |
