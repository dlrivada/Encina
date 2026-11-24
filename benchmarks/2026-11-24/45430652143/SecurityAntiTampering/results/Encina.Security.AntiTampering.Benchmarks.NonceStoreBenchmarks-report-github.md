```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,726.05 ns |      92.526 ns |     95.017 ns |     1,729.27 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        65.42 ns |       0.069 ns |      0.071 ns |        65.41 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        26.33 ns |       0.085 ns |      0.097 ns |        26.36 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,432,999.25 ns |  20,084.493 ns | 23,129.332 ns | 5,427,951.79 ns | 3,157.02 |  176.31 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     2,024.81 ns |  16,969.535 ns |    930.158 ns |     1,522.05 ns |     1.13 |    0.59 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        59.72 ns |       3.007 ns |      0.165 ns |        59.64 ns |     0.03 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        26.52 ns |       2.359 ns |      0.129 ns |        26.55 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,436,701.90 ns | 848,816.725 ns | 46,526.509 ns | 5,412,289.20 ns | 3,021.78 |  953.24 | 7.8125 |      - |  209640 B |    1,455.83 |
