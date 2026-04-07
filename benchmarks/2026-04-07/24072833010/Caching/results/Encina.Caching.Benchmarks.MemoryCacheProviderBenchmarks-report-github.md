```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         81.77 ns |     1.594 ns |   1.054 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.41 ns |     0.182 ns |   0.109 ns |  0.65 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,631.19 ns |   461.883 ns | 274.859 ns | 44.41 |    3.23 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         62.83 ns |     0.077 ns |   0.051 ns |  0.77 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         55.74 ns |     0.088 ns |   0.052 ns |  0.68 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        125.51 ns |     1.925 ns |   1.274 ns |  1.54 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,594.46 ns |   704.212 ns | 419.065 ns | 56.20 |    4.91 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,717.07 ns |    23.411 ns |  13.932 ns | 33.23 |    0.44 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,100.93 ns | 1,552.349 ns | 923.778 ns | 50.16 |   10.73 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |             |                  |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,992,204.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,006,801.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,441,351.00 ns |           NA |   0.000 ns |  0.86 |    0.00 |      - |      - |     712 B |        4.94 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,461,829.00 ns |           NA |   0.000 ns |  0.87 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,466,438.00 ns |           NA |   0.000 ns |  0.87 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  6,386,962.00 ns |           NA |   0.000 ns |  1.60 |    0.00 |      - |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,137,853.00 ns |           NA |   0.000 ns |  3.04 |    0.00 |      - |      - |    1080 B |        7.50 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,324,241.00 ns |           NA |   0.000 ns |  2.09 |    0.00 |      - |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,317,098.00 ns |           NA |   0.000 ns |  1.08 |    0.00 |      - |      - |     720 B |        5.00 |
