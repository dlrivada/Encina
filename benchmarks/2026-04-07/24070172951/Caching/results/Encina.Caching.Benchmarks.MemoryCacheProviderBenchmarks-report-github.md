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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |    80.23 ns |     0.339 ns |   0.224 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |    53.41 ns |     0.115 ns |   0.076 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3,586.71 ns |   346.400 ns | 206.137 ns | 44.71 |    2.44 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |    62.69 ns |     0.144 ns |   0.086 ns |  0.78 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |    53.25 ns |     0.143 ns |   0.094 ns |  0.66 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |   127.43 ns |     0.352 ns |   0.209 ns |  1.59 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 4,856.20 ns |   516.340 ns | 307.266 ns | 60.53 |    3.63 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 2,692.44 ns |    22.399 ns |  14.815 ns | 33.56 |    0.20 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3,728.22 ns | 1,135.414 ns | 675.667 ns | 46.47 |    7.99 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |    80.58 ns |     1.849 ns |   0.101 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |    54.94 ns |     0.308 ns |   0.017 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3,383.59 ns | 1,545.023 ns |  84.688 ns | 41.99 |    0.91 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |    60.20 ns |     0.887 ns |   0.049 ns |  0.75 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |    52.43 ns |     1.458 ns |   0.080 ns |  0.65 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |   125.95 ns |     8.403 ns |   0.461 ns |  1.56 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 4,645.13 ns | 2,833.490 ns | 155.313 ns | 57.65 |    1.67 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 2,735.08 ns |   364.754 ns |  19.993 ns | 33.94 |    0.22 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3,290.28 ns | 4,842.126 ns | 265.413 ns | 40.83 |    2.85 | 0.0420 | 0.0381 |     720 B |        5.00 |
