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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |    86.34 ns |     0.675 ns |   0.447 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |    53.66 ns |     0.273 ns |   0.162 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3,850.04 ns |   551.483 ns | 328.179 ns | 44.59 |    3.61 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |    60.61 ns |     0.113 ns |   0.067 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |    52.49 ns |     0.091 ns |   0.060 ns |  0.61 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |   128.28 ns |     1.965 ns |   1.300 ns |  1.49 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 4,665.39 ns |   639.000 ns | 380.259 ns | 54.04 |    4.18 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 2,760.07 ns |    25.792 ns |  17.060 ns | 31.97 |    0.25 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 4,033.49 ns | 1,325.302 ns | 788.666 ns | 46.72 |    8.66 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |    86.29 ns |     4.400 ns |   0.241 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |    55.07 ns |     2.282 ns |   0.125 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3,516.09 ns | 2,945.861 ns | 161.473 ns | 40.75 |    1.62 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |    61.72 ns |     0.273 ns |   0.015 ns |  0.72 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |    52.34 ns |     1.173 ns |   0.064 ns |  0.61 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |   141.67 ns |     7.533 ns |   0.413 ns |  1.64 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 4,618.00 ns | 2,301.555 ns | 126.156 ns | 53.52 |    1.27 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 2,770.97 ns |   325.317 ns |  17.832 ns | 32.11 |    0.20 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3,481.69 ns | 4,819.624 ns | 264.180 ns | 40.35 |    2.65 | 0.0420 | 0.0381 |     720 B |        5.00 |
