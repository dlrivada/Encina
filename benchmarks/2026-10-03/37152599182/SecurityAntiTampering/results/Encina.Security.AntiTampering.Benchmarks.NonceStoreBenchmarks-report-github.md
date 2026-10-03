```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       775.34 ns |      48.490 ns |     51.883 ns |       784.83 ns |     1.00 |     0.09 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        38.17 ns |       0.140 ns |      0.138 ns |        38.12 ns |     0.05 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        15.71 ns |       0.015 ns |      0.015 ns |        15.70 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,296,770.73 ns |  10,478.471 ns | 11,646.793 ns | 5,295,691.23 ns | 6,860.68 |   449.77 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,196.71 ns |  15,982.444 ns |    876.052 ns |       696.98 ns |     1.34 |     1.09 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        38.28 ns |       3.336 ns |      0.183 ns |        38.18 ns |     0.04 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        15.48 ns |       0.403 ns |      0.022 ns |        15.47 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,256,160.80 ns | 323,201.641 ns | 17,715.773 ns | 5,264,168.25 ns | 5,865.31 | 2,614.52 |  209656 B |    1,455.94 |
