```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       858.14 ns |      59.045 ns |     63.178 ns |       848.93 ns |     1.01 |     0.10 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        47.71 ns |       0.334 ns |      0.372 ns |        47.60 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        19.41 ns |       0.475 ns |      0.547 ns |        19.54 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,317,247.57 ns |  12,589.178 ns | 13,992.838 ns | 5,319,758.94 ns | 6,227.38 |   437.25 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,152.62 ns |  11,193.156 ns |    613.535 ns |       810.48 ns |     1.17 |     0.70 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        49.74 ns |       2.749 ns |      0.151 ns |        49.81 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.18 ns |       3.851 ns |      0.211 ns |        21.28 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,308,824.12 ns | 247,330.383 ns | 13,557.013 ns | 5,312,557.44 ns | 5,384.47 | 1,900.79 |  209656 B |    1,455.94 |
