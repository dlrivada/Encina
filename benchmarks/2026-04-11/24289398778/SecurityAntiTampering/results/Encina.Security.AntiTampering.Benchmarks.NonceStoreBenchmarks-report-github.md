```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,259.50 ns |     67.185 ns |     68.994 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        59.92 ns |      0.035 ns |      0.039 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.28 ns |      0.009 ns |      0.010 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,421,757.35 ns | 14,066.582 ns | 16,199.097 ns | 4,316.89 |  231.11 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,266.04 ns |     44.860 ns |     59.886 ns |     1.00 |    0.07 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        56.04 ns |      0.064 ns |      0.094 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        22.44 ns |      0.022 ns |      0.032 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,419,776.86 ns | 14,492.088 ns | 21,242.316 ns | 4,290.16 |  201.47 | 7.8125 |      - |  209656 B |    1,455.94 |
