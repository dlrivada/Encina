```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       788.32 ns |      44.792 ns |     47.927 ns |       775.18 ns |     1.00 |     0.08 |      - |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        47.64 ns |       0.044 ns |      0.049 ns |        47.61 ns |     0.06 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        15.80 ns |       0.023 ns |      0.026 ns |        15.79 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,448,496.66 ns |  18,309.497 ns | 21,085.244 ns | 5,449,990.80 ns | 6,935.28 |   403.45 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,193.28 ns |  11,685.788 ns |    640.537 ns |       829.80 ns |     1.17 |     0.71 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        46.40 ns |       0.696 ns |      0.038 ns |        46.41 ns |     0.05 |     0.02 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        16.45 ns |       0.297 ns |      0.016 ns |        16.45 ns |     0.02 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,538,692.56 ns | 470,270.046 ns | 25,777.088 ns | 5,525,397.47 ns | 5,439.41 | 1,931.07 |      - |  209640 B |    1,455.83 |
