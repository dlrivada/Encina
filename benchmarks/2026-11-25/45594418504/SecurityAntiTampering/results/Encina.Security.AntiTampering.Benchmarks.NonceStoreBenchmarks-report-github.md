```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,391.12 ns |      81.230 ns |     83.418 ns |     1,418.17 ns |     1.00 |     0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        54.90 ns |       0.153 ns |      0.176 ns |        54.90 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        23.97 ns |       0.112 ns |      0.125 ns |        23.92 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,382,845.60 ns |  15,104.082 ns | 17,393.884 ns | 5,383,568.00 ns | 3,882.74 |   230.13 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,713.47 ns |  15,559.287 ns |    852.857 ns |     1,242.97 ns |     1.15 |     0.65 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        54.93 ns |       4.154 ns |      0.228 ns |        54.97 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        24.19 ns |       8.582 ns |      0.470 ns |        24.04 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,397,724.85 ns | 369,702.685 ns | 20,264.652 ns | 5,388,805.18 ns | 3,614.43 | 1,212.29 | 7.8125 |      - |  209640 B |    1,455.83 |
