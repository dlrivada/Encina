```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.78 ns |     0.065 ns |   0.075 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    59.77 ns |     0.105 ns |   0.117 ns |  0.72 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    83.02 ns |     0.171 ns |   0.197 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    51.25 ns |     0.081 ns |   0.086 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   129.39 ns |     0.530 ns |   0.589 ns |  1.56 |    0.01 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,165.65 ns |   506.368 ns | 541.808 ns | 62.22 |    6.35 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,701.15 ns |    20.485 ns |  23.591 ns | 32.54 |    0.29 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,009.96 ns |   545.921 ns | 606.790 ns | 48.30 |    7.12 | 0.0305 | 0.0267 |     839 B |        5.83 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,576.85 ns |   269.484 ns | 288.345 ns | 43.09 |    3.38 | 0.0267 | 0.0229 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    50.98 ns |     3.185 ns |   0.175 ns |  0.59 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    58.72 ns |     0.892 ns |   0.049 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    85.86 ns |     4.162 ns |   0.228 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    51.92 ns |     0.899 ns |   0.049 ns |  0.60 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   131.49 ns |     1.393 ns |   0.076 ns |  1.53 |    0.00 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,304.13 ns | 7,265.814 ns | 398.264 ns | 50.13 |    4.02 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,723.07 ns |   117.567 ns |   6.444 ns | 31.71 |    0.10 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,377.19 ns | 3,758.677 ns | 206.026 ns | 39.33 |    2.08 | 0.0267 | 0.0229 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,248.07 ns | 4,560.480 ns | 249.975 ns | 37.83 |    2.52 | 0.0267 | 0.0229 |     712 B |        4.94 |
