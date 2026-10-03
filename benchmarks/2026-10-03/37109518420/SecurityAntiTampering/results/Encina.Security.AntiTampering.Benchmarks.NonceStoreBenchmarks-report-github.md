```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev       | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|-------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       906.04 ns |     48.652 ns |    52.058 ns |       888.61 ns |     1.00 |     0.08 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        52.84 ns |      0.017 ns |     0.017 ns |        52.84 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        21.22 ns |      0.131 ns |     0.129 ns |        21.27 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,322,334.60 ns |  5,906.225 ns | 6,801.617 ns | 5,323,773.50 ns | 5,892.30 |   323.15 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |              |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,222.18 ns | 11,954.968 ns |   655.292 ns |       853.14 ns |     1.17 |     0.71 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        54.20 ns |      1.929 ns |     0.106 ns |        54.15 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.60 ns |      1.234 ns |     0.068 ns |        21.64 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,334,511.95 ns | 75,430.216 ns | 4,134.585 ns | 5,332,343.40 ns | 5,113.36 | 1,814.11 |  209656 B |    1,455.94 |
