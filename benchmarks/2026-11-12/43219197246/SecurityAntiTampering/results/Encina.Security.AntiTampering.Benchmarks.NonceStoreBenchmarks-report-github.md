```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       797.38 ns |      40.778 ns |     43.632 ns |       800.95 ns |     1.00 |     0.08 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        45.73 ns |       0.087 ns |      0.089 ns |        45.73 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        17.28 ns |       0.075 ns |      0.083 ns |        17.29 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,308,922.86 ns |  30,052.167 ns | 34,608.120 ns | 5,290,850.39 ns | 6,676.69 |   355.74 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,102.05 ns |  11,402.546 ns |    625.012 ns |       749.91 ns |     1.19 |     0.76 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        45.66 ns |      16.153 ns |      0.885 ns |        45.18 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        18.86 ns |       2.241 ns |      0.123 ns |        18.82 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,286,109.32 ns | 241,224.871 ns | 13,222.349 ns | 5,289,048.89 ns | 5,721.17 | 2,118.21 |  209656 B |    1,455.94 |
