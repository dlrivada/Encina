```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev       | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|-------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,109.11 ns |     90.952 ns |    97.317 ns |     1,112.98 ns |     1.01 |     0.12 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        37.33 ns |      0.368 ns |     0.409 ns |        37.16 ns |     0.03 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        12.85 ns |      0.161 ns |     0.179 ns |        12.79 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,245,082.44 ns |  8,281.657 ns | 8,861.281 ns | 5,245,476.52 ns | 4,764.67 |   421.08 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |              |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,343.69 ns | 12,442.872 ns |   682.036 ns |       961.27 ns |     1.15 |     0.66 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        37.22 ns |      1.960 ns |     0.107 ns |        37.27 ns |     0.03 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        13.24 ns |      1.866 ns |     0.102 ns |        13.25 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,266,525.66 ns | 56,357.115 ns | 3,089.124 ns | 5,268,123.77 ns | 4,520.17 | 1,537.77 | 7.8125 |      - |  209656 B |    1,455.94 |
