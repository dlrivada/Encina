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
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       913.94 ns |      55.758 ns |     59.660 ns |       903.78 ns |     1.00 |     0.09 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        51.70 ns |       0.216 ns |      0.248 ns |        51.75 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.98 ns |       0.131 ns |      0.151 ns |        20.98 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,328,585.47 ns |  12,965.254 ns | 14,410.846 ns | 5,323,707.39 ns | 5,853.47 |   365.62 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,236.56 ns |  12,150.260 ns |    665.997 ns |       854.05 ns |     1.17 |     0.71 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        51.93 ns |       4.835 ns |      0.265 ns |        52.01 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.31 ns |       0.869 ns |      0.048 ns |        21.33 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,352,537.51 ns | 234,967.200 ns | 12,879.345 ns | 5,346,494.95 ns | 5,077.63 | 1,806.68 |  209656 B |    1,455.94 |
