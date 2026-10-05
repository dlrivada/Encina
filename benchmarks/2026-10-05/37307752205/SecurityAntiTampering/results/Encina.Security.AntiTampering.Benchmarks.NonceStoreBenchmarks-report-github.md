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
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       912.26 ns |      57.155 ns |     61.155 ns |       906.64 ns |     1.00 |     0.09 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        51.66 ns |       0.238 ns |      0.265 ns |        51.70 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        21.91 ns |       0.125 ns |      0.134 ns |        21.90 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,343,128.48 ns |  10,545.579 ns | 12,144.305 ns | 5,345,490.88 ns | 5,881.90 |   383.34 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,284.70 ns |  13,560.902 ns |    743.319 ns |       860.67 ns |     1.20 |     0.78 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        51.29 ns |       2.686 ns |      0.147 ns |        51.30 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        20.67 ns |       2.569 ns |      0.141 ns |        20.74 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,356,010.62 ns | 248,610.552 ns | 13,627.183 ns | 5,356,271.08 ns | 5,006.77 | 1,880.90 |  209656 B |    1,455.94 |
