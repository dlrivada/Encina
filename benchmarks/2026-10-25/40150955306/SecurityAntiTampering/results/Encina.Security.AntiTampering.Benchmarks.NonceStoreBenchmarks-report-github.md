```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,033.27 ns |     69.152 ns |     71.014 ns |     1,045.68 ns |     1.00 |    0.10 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        36.94 ns |      0.217 ns |      0.241 ns |        36.85 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        13.46 ns |      0.147 ns |      0.158 ns |        13.47 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,266,531.81 ns | 10,014.346 ns | 11,532.535 ns | 5,267,603.00 ns | 5,120.81 |  359.58 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,077.67 ns |     60.614 ns |     86.931 ns |     1,070.40 ns |     1.01 |    0.11 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        38.57 ns |      0.787 ns |      1.177 ns |        38.95 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        14.55 ns |      1.046 ns |      1.467 ns |        15.55 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,297,973.23 ns | 18,963.169 ns | 28,383.181 ns | 5,290,426.45 ns | 4,946.15 |  381.34 | 7.8125 |      - |  209656 B |    1,455.94 |
