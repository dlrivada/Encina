```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error            | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|-----------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,424.56 ns |        80.412 ns |     82.578 ns |     1,451.83 ns |     1.00 |     0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        55.33 ns |         0.112 ns |      0.125 ns |        55.28 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        24.01 ns |         0.068 ns |      0.076 ns |        23.98 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,396,341.05 ns |    13,383.895 ns | 15,412.913 ns | 5,397,583.15 ns | 3,800.17 |   216.44 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                  |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,722.94 ns |    15,905.348 ns |    871.826 ns |     1,246.63 ns |     1.15 |     0.66 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        56.14 ns |         3.991 ns |      0.219 ns |        56.11 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        23.83 ns |         0.332 ns |      0.018 ns |        23.83 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,480,468.23 ns | 1,118,728.701 ns | 61,321.295 ns | 5,471,985.52 ns | 3,666.03 | 1,246.93 | 7.8125 |      - |  209640 B |    1,455.83 |
