```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    33.92 ns |     0.300 ns |   0.321 ns |  0.75 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    38.22 ns |     0.239 ns |   0.245 ns |  0.84 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    45.34 ns |     0.226 ns |   0.242 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    34.85 ns |     0.075 ns |   0.084 ns |  0.77 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    63.80 ns |     0.402 ns |   0.430 ns |  1.41 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,759.91 ns |   329.676 ns | 352.750 ns | 82.92 |    7.58 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,606.33 ns |    12.834 ns |  14.780 ns | 35.43 |    0.37 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 2,987.00 ns |   299.966 ns | 333.411 ns | 65.88 |    7.18 | 0.0496 | 0.0458 |     853 B |        5.92 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,090.33 ns |   214.153 ns | 229.141 ns | 68.16 |    4.93 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    36.34 ns |    82.743 ns |   4.535 ns |  0.77 |    0.09 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    38.04 ns |     0.835 ns |   0.046 ns |  0.81 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    47.09 ns |    39.752 ns |   2.179 ns |  1.00 |    0.06 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    34.97 ns |     1.564 ns |   0.086 ns |  0.74 |    0.03 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    63.99 ns |     3.014 ns |   0.165 ns |  1.36 |    0.05 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,343.16 ns | 1,192.115 ns |  65.344 ns | 71.09 |    3.03 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,613.62 ns |   124.009 ns |   6.797 ns | 34.31 |    1.35 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,534.23 ns | 4,864.485 ns | 266.639 ns | 53.89 |    5.35 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,774.71 ns | 1,367.195 ns |  74.941 ns | 59.00 |    2.69 | 0.0420 | 0.0381 |     712 B |        4.94 |
