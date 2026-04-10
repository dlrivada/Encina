```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method      | Job        | IterationCount | LaunchCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |----------------:|---------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     |     1,517.55 ns |     248.026 ns |    129.723 ns |     1,498.28 ns |     1.01 |    0.11 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     |        59.78 ns |       0.037 ns |      0.022 ns |        59.78 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     |        25.71 ns |       0.055 ns |      0.033 ns |        25.70 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | 5,428,515.89 ns |  11,634.356 ns |  6,084.993 ns | 5,430,685.59 ns | 3,599.06 |  277.07 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           |     1,831.03 ns |  14,202.785 ns |    778.503 ns |     1,412.66 ns |     1.11 |    0.54 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           |        59.59 ns |       0.890 ns |      0.049 ns |        59.57 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           |        25.74 ns |       0.076 ns |      0.004 ns |        25.74 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 5,427,071.81 ns | 412,289.683 ns | 22,598.989 ns | 5,424,063.09 ns | 3,282.26 |  973.37 | 7.8125 |      - |  209656 B |    1,455.94 |
