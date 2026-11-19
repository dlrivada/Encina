```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       918.75 ns |      52.815 ns |     54.237 ns |       930.82 ns |     1.00 |     0.08 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        47.45 ns |       0.017 ns |      0.018 ns |        47.45 ns |     0.05 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        16.12 ns |       0.021 ns |      0.023 ns |        16.11 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,522,857.11 ns |  16,433.689 ns | 18,266.001 ns | 5,525,431.69 ns | 6,031.36 |   353.05 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,129.72 ns |  11,088.548 ns |    607.801 ns |       826.98 ns |     1.18 |     0.72 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        47.74 ns |       0.080 ns |      0.004 ns |        47.74 ns |     0.05 |     0.02 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        16.99 ns |       0.075 ns |      0.004 ns |        16.99 ns |     0.02 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,457,184.56 ns | 349,672.208 ns | 19,166.714 ns | 5,465,292.39 ns | 5,676.47 | 2,053.35 |      - |  209640 B |    1,455.83 |
