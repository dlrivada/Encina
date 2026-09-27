```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       925.75 ns |      64.260 ns |     68.758 ns |       942.17 ns |     1.01 |     0.10 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        39.02 ns |       0.545 ns |      0.535 ns |        38.68 ns |     0.04 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        16.01 ns |       0.656 ns |      0.756 ns |        15.65 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,297,384.09 ns |  14,099.028 ns | 15,085.802 ns | 5,298,134.32 ns | 5,752.44 |   419.52 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,196.86 ns |  12,595.993 ns |    690.429 ns |       817.25 ns |     1.20 |     0.78 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        38.14 ns |       0.200 ns |      0.011 ns |        38.13 ns |     0.04 |     0.01 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        15.76 ns |       9.223 ns |      0.506 ns |        15.48 ns |     0.02 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,265,382.34 ns | 146,470.955 ns |  8,028.567 ns | 5,267,406.06 ns | 5,279.42 | 1,983.52 |      - |  209656 B |    1,455.94 |
