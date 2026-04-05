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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         87.21 ns |     0.841 ns |   0.557 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.89 ns |     0.195 ns |   0.129 ns |  0.65 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,908.26 ns |   398.450 ns | 237.111 ns | 44.82 |    2.59 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         61.01 ns |     0.166 ns |   0.110 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         52.33 ns |     0.101 ns |   0.067 ns |  0.60 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        130.89 ns |     4.531 ns |   2.997 ns |  1.50 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,864.25 ns |   809.672 ns | 481.823 ns | 55.78 |    5.25 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,829.36 ns |    15.816 ns |   8.272 ns | 32.44 |    0.22 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,916.38 ns | 1,057.910 ns | 629.545 ns | 44.91 |    6.85 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |             |                  |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,114,374.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,122,400.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,493,094.00 ns |           NA |   0.000 ns |  0.85 |    0.00 |      - |      - |     712 B |        4.94 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,599,563.00 ns |           NA |   0.000 ns |  0.87 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,516,708.00 ns |           NA |   0.000 ns |  0.85 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  6,460,176.00 ns |           NA |   0.000 ns |  1.57 |    0.00 |      - |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,266,340.00 ns |           NA |   0.000 ns |  2.98 |    0.00 |      - |      - |    1080 B |        7.50 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,474,207.00 ns |           NA |   0.000 ns |  2.06 |    0.00 |      - |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,388,136.00 ns |           NA |   0.000 ns |  1.07 |    0.00 |      - |      - |     720 B |        5.00 |
