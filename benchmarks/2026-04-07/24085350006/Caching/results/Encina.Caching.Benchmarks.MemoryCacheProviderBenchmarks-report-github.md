```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |    83.70 ns |     1.759 ns |   1.164 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |    53.39 ns |     0.094 ns |   0.062 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3,681.76 ns |   531.130 ns | 316.067 ns | 43.99 |    3.63 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |    60.25 ns |     0.119 ns |   0.062 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |    52.43 ns |     0.045 ns |   0.027 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |   128.75 ns |     2.944 ns |   1.947 ns |  1.54 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 4,985.01 ns |   523.694 ns | 311.642 ns | 59.57 |    3.62 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 2,742.83 ns |    31.804 ns |  21.037 ns | 32.77 |    0.50 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 4,102.80 ns | 1,380.540 ns | 821.537 ns | 49.03 |    9.33 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |    83.54 ns |    18.991 ns |   1.041 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |    53.39 ns |     0.607 ns |   0.033 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3,429.42 ns | 2,355.566 ns | 129.117 ns | 41.06 |    1.41 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |    60.36 ns |     3.841 ns |   0.211 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |    52.96 ns |     0.640 ns |   0.035 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |   130.62 ns |    29.949 ns |   1.642 ns |  1.56 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 4,864.19 ns |   782.597 ns |  42.897 ns | 58.23 |    0.77 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 2,722.95 ns |   199.623 ns |  10.942 ns | 32.60 |    0.37 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3,444.70 ns | 5,514.692 ns | 302.279 ns | 41.24 |    3.17 | 0.0420 | 0.0381 |     720 B |        5.00 |
