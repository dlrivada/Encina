```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       892.83 ns |      49.250 ns |     52.697 ns |       885.24 ns |     1.00 |     0.08 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        51.67 ns |       0.091 ns |      0.094 ns |        51.66 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.56 ns |       0.147 ns |      0.169 ns |        22.56 ns |     0.03 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,323,455.76 ns |  11,304.564 ns | 13,018.353 ns | 5,324,830.11 ns | 5,982.01 |   341.33 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,335.43 ns |  16,355.370 ns |    896.493 ns |       826.26 ns |     1.28 |     0.96 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        53.39 ns |       3.400 ns |      0.186 ns |        53.37 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.22 ns |       2.403 ns |      0.132 ns |        21.19 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,343,358.96 ns | 259,824.645 ns | 14,241.866 ns | 5,339,757.02 ns | 5,107.37 | 2,140.82 |  209656 B |    1,455.94 |
