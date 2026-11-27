```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       797.34 ns |      53.987 ns |     57.765 ns |       788.97 ns |     1.00 |     0.10 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        46.21 ns |       0.027 ns |      0.030 ns |        46.21 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        15.86 ns |       0.043 ns |      0.048 ns |        15.86 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,487,214.76 ns |  23,718.433 ns | 27,314.183 ns | 5,482,739.52 ns | 6,915.47 |   479.82 |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,164.78 ns |  14,535.169 ns |    796.722 ns |       710.02 ns |     1.29 |     0.99 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        46.04 ns |       0.943 ns |      0.052 ns |        46.05 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        15.98 ns |       1.084 ns |      0.059 ns |        15.98 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,540,516.41 ns | 112,381.270 ns |  6,159.997 ns | 5,538,215.59 ns | 6,126.91 | 2,602.43 |  209640 B |    1,455.83 |
