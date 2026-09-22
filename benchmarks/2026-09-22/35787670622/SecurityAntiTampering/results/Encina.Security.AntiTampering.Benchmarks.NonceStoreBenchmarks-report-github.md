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
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       931.91 ns |      56.033 ns |     59.954 ns |       919.89 ns |     1.00 |     0.09 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        52.69 ns |       0.037 ns |      0.041 ns |        52.71 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.86 ns |       0.102 ns |      0.100 ns |        22.87 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,338,617.33 ns |   8,687.054 ns | 10,004.024 ns | 5,340,434.15 ns | 5,750.91 |   356.54 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,259.08 ns |  12,303.288 ns |    674.385 ns |       881.63 ns |     1.17 |     0.71 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        53.53 ns |       6.259 ns |      0.343 ns |        53.36 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.50 ns |       4.251 ns |      0.233 ns |        21.53 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,366,650.82 ns | 261,442.280 ns | 14,330.534 ns | 5,372,146.91 ns | 4,992.07 | 1,770.31 |  209656 B |    1,455.94 |
