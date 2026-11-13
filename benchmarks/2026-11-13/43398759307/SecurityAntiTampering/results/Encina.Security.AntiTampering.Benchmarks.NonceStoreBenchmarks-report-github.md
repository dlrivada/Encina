```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,098.77 ns |     108.034 ns |    115.595 ns |     1,094.58 ns |     1.01 |     0.15 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        37.77 ns |       0.565 ns |      0.629 ns |        37.98 ns |     0.03 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        12.15 ns |       0.157 ns |      0.174 ns |        12.08 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,295,974.44 ns |  18,815.873 ns | 20,913.793 ns | 5,291,193.76 ns | 4,870.41 |   498.52 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,263.21 ns |  12,120.483 ns |    664.365 ns |       909.04 ns |     1.17 |     0.69 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        35.46 ns |       1.147 ns |      0.063 ns |        35.45 ns |     0.03 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        12.15 ns |       1.528 ns |      0.084 ns |        12.10 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,318,325.60 ns | 402,816.914 ns | 22,079.754 ns | 5,315,970.45 ns | 4,906.85 | 1,723.66 | 7.8125 |      - |  209640 B |    1,455.83 |
