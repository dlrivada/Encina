```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev       | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|-------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,260.64 ns |      67.775 ns |    69.600 ns |     1,271.54 ns |     1.00 |     0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        60.22 ns |       0.117 ns |     0.130 ns |        60.23 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        27.48 ns |       0.039 ns |     0.044 ns |        27.47 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,429,123.46 ns |   8,604.187 ns | 9,563.531 ns | 5,428,731.59 ns | 4,318.93 |   231.03 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |              |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,617.67 ns |  13,863.284 ns |   759.893 ns |     1,220.04 ns |     1.13 |     0.60 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        56.01 ns |       1.042 ns |     0.057 ns |        56.00 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        22.28 ns |       0.229 ns |     0.013 ns |        22.28 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,437,881.25 ns | 130,638.887 ns | 7,160.758 ns | 5,435,552.89 ns | 3,803.84 | 1,225.22 | 7.8125 |      - |  209656 B |    1,455.94 |
