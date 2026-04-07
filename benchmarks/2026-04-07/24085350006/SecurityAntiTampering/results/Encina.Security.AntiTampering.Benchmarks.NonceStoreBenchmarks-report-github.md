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
| Add         | Job-YFEFPZ | 10             | Default     |     1,269.93 ns |     192.955 ns |    100.919 ns |     1,241.99 ns |     1.01 |     0.10 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     |        56.43 ns |       0.223 ns |      0.147 ns |        56.43 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     |        22.33 ns |       0.094 ns |      0.056 ns |        22.30 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | 5,442,716.81 ns |  32,846.565 ns | 21,725.969 ns | 5,445,126.97 ns | 4,308.55 |   308.91 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           |     1,591.82 ns |  13,721.728 ns |    752.134 ns |     1,184.10 ns |     1.13 |     0.61 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           |        60.43 ns |       3.215 ns |      0.176 ns |        60.35 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           |        22.25 ns |       0.475 ns |      0.026 ns |        22.24 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 5,424,182.74 ns | 203,845.237 ns | 11,173.445 ns | 5,429,179.19 ns | 3,859.81 | 1,244.43 | 7.8125 |      - |  209656 B |    1,455.94 |
