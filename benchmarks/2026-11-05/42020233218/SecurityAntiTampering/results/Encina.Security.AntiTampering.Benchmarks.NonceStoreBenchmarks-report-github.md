```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       949.10 ns |      63.093 ns |     67.509 ns |       931.30 ns |     1.00 |     0.10 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        53.18 ns |       0.027 ns |      0.028 ns |        53.19 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.24 ns |       0.124 ns |      0.143 ns |        20.19 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,417,746.08 ns |  22,738.959 ns | 26,186.219 ns | 5,416,638.98 ns | 5,734.96 |   387.76 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,318.36 ns |  14,004.642 ns |    767.642 ns |       887.93 ns |     1.20 |     0.79 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        54.16 ns |       0.641 ns |      0.035 ns |        54.17 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.23 ns |       0.794 ns |      0.043 ns |        21.22 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,378,948.77 ns | 307,007.061 ns | 16,828.093 ns | 5,372,605.55 ns | 4,911.33 | 1,855.29 |  209656 B |    1,455.94 |
