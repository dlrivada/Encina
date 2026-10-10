```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev       | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|-------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,079.45 ns |     66.319 ns |    68.105 ns |     1,090.09 ns |     1.00 |     0.09 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        55.72 ns |      0.048 ns |     0.050 ns |        55.70 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.35 ns |      0.012 ns |     0.012 ns |        22.35 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,384,381.65 ns |  8,937.410 ns | 9,933.908 ns | 5,383,927.40 ns | 5,006.71 |   306.45 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |              |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,422.13 ns | 14,936.825 ns |   818.738 ns |       971.22 ns |     1.20 |     0.78 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        53.21 ns |      0.412 ns |     0.023 ns |        53.20 ns |     0.04 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        23.28 ns |      0.293 ns |     0.016 ns |        23.29 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,430,144.52 ns | 66,771.879 ns | 3,659.992 ns | 5,431,399.11 ns | 4,578.88 | 1,717.44 | 7.8125 |      - |  209656 B |    1,455.94 |
