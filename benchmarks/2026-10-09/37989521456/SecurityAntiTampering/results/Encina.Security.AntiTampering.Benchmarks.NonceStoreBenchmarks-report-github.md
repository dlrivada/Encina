```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev       | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|-------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,012.14 ns |     69.923 ns |    74.817 ns |     1,020.16 ns |     1.01 |     0.10 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        35.46 ns |      0.441 ns |     0.472 ns |        35.49 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        12.56 ns |      0.362 ns |     0.417 ns |        12.82 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,280,630.54 ns |  7,161.066 ns | 8,246.694 ns | 5,281,855.73 ns | 5,244.66 |   383.34 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |              |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,191.35 ns | 10,833.988 ns |   593.847 ns |       872.30 ns |     1.15 |     0.65 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        37.42 ns |      3.059 ns |     0.168 ns |        37.34 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        12.29 ns |      0.428 ns |     0.023 ns |        12.30 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,243,449.20 ns | 55,454.804 ns | 3,039.665 ns | 5,243,006.72 ns | 5,053.06 | 1,700.62 | 7.8125 |      - |  209656 B |    1,455.94 |
