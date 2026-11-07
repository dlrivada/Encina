```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       795.61 ns |      44.999 ns |     48.148 ns |       813.61 ns |     1.00 |     0.08 |      - |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        40.71 ns |       0.996 ns |      1.147 ns |        40.24 ns |     0.05 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        15.53 ns |       0.405 ns |      0.466 ns |        15.46 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,273,041.03 ns |  15,097.776 ns | 17,386.622 ns | 5,275,604.77 ns | 6,651.12 |   400.79 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,319.08 ns |  14,751.496 ns |    808.579 ns |       874.05 ns |     1.23 |     0.85 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        41.22 ns |      29.952 ns |      1.642 ns |        40.50 ns |     0.04 |     0.02 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        15.26 ns |       4.658 ns |      0.255 ns |        15.16 ns |     0.01 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,313,364.32 ns | 219,771.335 ns | 12,046.409 ns | 5,315,578.28 ns | 4,944.55 | 1,944.05 |      - |  209656 B |    1,455.94 |
