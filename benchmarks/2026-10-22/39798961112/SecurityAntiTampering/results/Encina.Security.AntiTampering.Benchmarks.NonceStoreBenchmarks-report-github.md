```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,258.65 ns |     69.284 ns |     71.149 ns |     1,268.82 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        56.18 ns |      0.078 ns |      0.087 ns |        56.14 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.89 ns |      0.277 ns |      0.319 ns |        23.04 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,444,957.35 ns | 12,544.449 ns | 14,446.206 ns | 5,444,502.08 ns | 4,339.05 |  239.06 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,266.58 ns |     35.351 ns |     45.967 ns |     1,276.37 ns |     1.00 |    0.05 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        55.87 ns |      0.243 ns |      0.333 ns |        55.85 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        22.33 ns |      0.054 ns |      0.076 ns |        22.39 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,405,393.78 ns |  9,505.883 ns | 14,227.959 ns | 5,406,324.93 ns | 4,273.32 |  158.79 | 7.8125 |      - |  209656 B |    1,455.94 |
