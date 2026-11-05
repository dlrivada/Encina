```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       929.08 ns |      69.864 ns |     74.753 ns |       929.13 ns |     1.01 |     0.11 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        45.25 ns |       0.013 ns |      0.015 ns |        45.25 ns |     0.05 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        15.58 ns |       0.015 ns |      0.017 ns |        15.58 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,475,034.62 ns |  23,324.809 ns | 26,860.885 ns | 5,476,323.75 ns | 5,928.76 |   460.72 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,189.60 ns |  11,144.529 ns |    610.869 ns |       853.85 ns |     1.16 |     0.67 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        45.24 ns |       0.276 ns |      0.015 ns |        45.24 ns |     0.04 |     0.02 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        15.78 ns |       0.400 ns |      0.022 ns |        15.77 ns |     0.02 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,505,787.88 ns | 215,469.847 ns | 11,810.629 ns | 5,503,818.10 ns | 5,355.48 | 1,840.79 |      - |  209656 B |    1,455.94 |
