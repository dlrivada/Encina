```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,016.56 ns |      88.040 ns |     94.202 ns |     1,016.95 ns |     1.01 |     0.13 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        35.42 ns |       0.537 ns |      0.574 ns |        35.11 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        12.96 ns |       0.111 ns |      0.114 ns |        12.93 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,293,240.76 ns |  16,387.423 ns | 18,214.577 ns | 5,293,520.83 ns | 5,248.32 |   464.10 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,253.89 ns |  12,008.809 ns |    658.243 ns |       909.38 ns |     1.17 |     0.69 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        35.07 ns |       3.490 ns |      0.191 ns |        34.96 ns |     0.03 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        13.00 ns |       0.871 ns |      0.048 ns |        13.00 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,332,816.09 ns | 236,686.961 ns | 12,973.611 ns | 5,337,413.98 ns | 4,955.55 | 1,742.60 | 7.8125 |      - |  209640 B |    1,455.83 |
