```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method      | Job        | IterationCount | LaunchCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     |     1,247.25 ns |     211.987 ns |    110.873 ns |     1,225.16 ns |     1.01 |     0.12 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     |        57.38 ns |       0.062 ns |      0.033 ns |        57.39 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     |        22.31 ns |       0.076 ns |      0.045 ns |        22.28 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | 5,399,562.36 ns |  27,420.856 ns | 18,137.198 ns | 5,401,381.32 ns | 4,357.72 |   346.94 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           |     1,546.54 ns |  13,020.689 ns |    713.708 ns |     1,157.94 ns |     1.13 |     0.59 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           |        56.56 ns |       4.484 ns |      0.246 ns |        56.64 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           |        22.32 ns |       0.055 ns |      0.003 ns |        22.32 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 5,431,534.43 ns | 500,433.424 ns | 27,430.445 ns | 5,420,864.31 ns | 3,956.38 | 1,251.62 | 7.8125 |      - |  209656 B |    1,455.94 |
