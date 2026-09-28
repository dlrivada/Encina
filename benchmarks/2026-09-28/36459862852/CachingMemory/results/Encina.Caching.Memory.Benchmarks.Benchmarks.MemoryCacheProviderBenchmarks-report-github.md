```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    62.30 ns |     0.142 ns |   0.158 ns |  0.79 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    64.51 ns |     0.061 ns |   0.065 ns |  0.82 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    78.91 ns |     0.108 ns |   0.120 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    57.55 ns |     0.055 ns |   0.059 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   114.20 ns |     0.072 ns |   0.074 ns |  1.45 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,461.08 ns |   582.411 ns | 623.173 ns | 69.21 |    7.69 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,998.38 ns |    17.357 ns |  19.292 ns | 38.00 |    0.24 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,985.18 ns |   416.052 ns | 445.171 ns | 50.50 |    5.49 | 0.0381 | 0.0305 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,933.97 ns |   363.637 ns | 389.088 ns | 49.85 |    4.80 | 0.0381 | 0.0305 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    55.96 ns |     0.616 ns |   0.034 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    66.01 ns |    15.212 ns |   0.834 ns |  0.82 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    80.11 ns |     2.445 ns |   0.134 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    56.99 ns |     2.754 ns |   0.151 ns |  0.71 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   115.08 ns |     0.434 ns |   0.024 ns |  1.44 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,809.06 ns | 3,319.697 ns | 181.964 ns | 60.03 |    1.97 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,966.38 ns |   149.275 ns |   8.182 ns | 37.03 |    0.10 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,798.64 ns | 5,424.210 ns | 297.319 ns | 47.42 |    3.21 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,941.96 ns | 1,109.492 ns |  60.815 ns | 49.21 |    0.66 | 0.0420 | 0.0381 |     712 B |        4.94 |
