```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    53.01 ns |     0.077 ns |   0.086 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.12 ns |     0.061 ns |   0.063 ns |  0.69 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    87.27 ns |     0.831 ns |   0.958 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    53.99 ns |     0.032 ns |   0.036 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   130.38 ns |     2.276 ns |   2.622 ns |  1.49 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,456.87 ns |   463.288 ns | 495.713 ns | 62.53 |    5.57 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,899.82 ns |    24.285 ns |  27.967 ns | 33.23 |    0.48 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,312.36 ns |   508.059 ns | 564.706 ns | 49.42 |    6.33 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,604.50 ns |   310.923 ns | 332.684 ns | 41.31 |    3.74 | 0.0381 | 0.0305 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    53.66 ns |     0.849 ns |   0.047 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    60.76 ns |     3.798 ns |   0.208 ns |  0.71 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    85.90 ns |     2.080 ns |   0.114 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    53.19 ns |     2.189 ns |   0.120 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   128.91 ns |    17.586 ns |   0.964 ns |  1.50 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,666.31 ns | 4,650.182 ns | 254.892 ns | 54.32 |    2.57 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,918.49 ns |   457.005 ns |  25.050 ns | 33.97 |    0.26 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,334.71 ns | 4,537.783 ns | 248.731 ns | 38.82 |    2.51 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,548.93 ns | 2,410.701 ns | 132.139 ns | 41.31 |    1.33 | 0.0420 | 0.0381 |     712 B |        4.94 |
