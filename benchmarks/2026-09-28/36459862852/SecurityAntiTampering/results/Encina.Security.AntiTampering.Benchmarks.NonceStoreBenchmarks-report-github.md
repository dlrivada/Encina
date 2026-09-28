```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       869.27 ns |     54.578 ns |     58.398 ns |       863.77 ns |     1.00 |     0.09 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        49.31 ns |      0.127 ns |      0.135 ns |        49.31 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.02 ns |      0.102 ns |      0.118 ns |        20.02 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,366,664.98 ns | 15,883.503 ns | 18,291.466 ns | 5,372,409.79 ns | 6,200.18 |   406.63 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,180.39 ns | 11,700.007 ns |    641.317 ns |       821.30 ns |     1.18 |     0.72 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        49.97 ns |      8.584 ns |      0.471 ns |        50.16 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.52 ns |      2.059 ns |      0.113 ns |        21.51 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,384,303.01 ns | 82,995.247 ns |  4,549.250 ns | 5,382,421.43 ns | 5,365.76 | 1,923.59 |  209656 B |    1,455.94 |
