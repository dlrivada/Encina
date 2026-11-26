```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,140.07 ns |    102.260 ns |    109.417 ns |     1,132.17 ns |     1.01 |     0.13 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        38.32 ns |      0.418 ns |      0.482 ns |        38.25 ns |     0.03 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        14.61 ns |      0.137 ns |      0.147 ns |        14.60 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,261,888.98 ns | 16,362.946 ns | 18,843.592 ns | 5,263,152.36 ns | 4,656.42 |   443.05 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |               |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,338.83 ns | 12,096.082 ns |    663.027 ns |       972.99 ns |     1.15 |     0.64 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        39.48 ns |      4.477 ns |      0.245 ns |        39.55 ns |     0.03 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        14.69 ns |      1.738 ns |      0.095 ns |        14.68 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,268,730.74 ns | 83,320.638 ns |  4,567.085 ns | 5,268,502.98 ns | 4,509.33 | 1,506.40 | 7.8125 |      - |  209640 B |    1,455.83 |
