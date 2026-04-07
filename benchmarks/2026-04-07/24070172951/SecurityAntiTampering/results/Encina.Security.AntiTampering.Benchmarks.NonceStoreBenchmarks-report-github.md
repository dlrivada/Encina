```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method      | Job        | IterationCount | LaunchCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     |       999.30 ns |     193.495 ns |    101.202 ns |       984.91 ns |     1.01 |     0.13 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     |        52.55 ns |       0.033 ns |      0.020 ns |        52.54 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     |        20.97 ns |       0.046 ns |      0.027 ns |        20.96 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | 5,301,271.57 ns |  18,195.163 ns | 12,034.974 ns | 5,300,194.20 ns | 5,348.48 |   464.80 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           |     1,351.47 ns |  14,005.435 ns |    767.685 ns |       935.44 ns |     1.19 |     0.76 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           |        51.35 ns |       0.642 ns |      0.035 ns |        51.34 ns |     0.05 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           |        20.67 ns |       0.321 ns |      0.018 ns |        20.67 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 5,313,766.09 ns | 283,981.783 ns | 15,566.000 ns | 5,316,038.91 ns | 4,694.33 | 1,746.01 | 7.8125 |      - |  209656 B |    1,455.94 |
