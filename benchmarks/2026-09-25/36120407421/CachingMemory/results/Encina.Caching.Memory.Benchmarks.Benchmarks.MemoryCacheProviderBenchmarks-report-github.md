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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    56.19 ns |     0.041 ns |   0.044 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    64.67 ns |     0.038 ns |   0.044 ns |  0.80 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    80.85 ns |     0.054 ns |   0.058 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    56.84 ns |     0.046 ns |   0.049 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   114.27 ns |     0.116 ns |   0.128 ns |  1.41 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,733.48 ns |   631.189 ns | 675.365 ns | 70.92 |    8.13 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,994.22 ns |    15.757 ns |  18.146 ns | 37.03 |    0.22 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,459.54 ns |   519.949 ns | 577.922 ns | 55.16 |    6.97 | 0.0458 | 0.0420 |     811 B |        5.63 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,208.87 ns |   223.380 ns | 229.395 ns | 52.06 |    2.76 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    58.69 ns |     0.585 ns |   0.032 ns |  0.74 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    65.27 ns |     1.798 ns |   0.099 ns |  0.82 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    79.47 ns |     2.437 ns |   0.134 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    59.64 ns |     0.452 ns |   0.025 ns |  0.75 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   114.48 ns |     0.669 ns |   0.037 ns |  1.44 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,777.64 ns | 3,608.193 ns | 197.777 ns | 60.12 |    2.16 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,969.73 ns |   428.381 ns |  23.481 ns | 37.37 |    0.26 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,264.53 ns | 5,244.713 ns | 287.480 ns | 41.08 |    3.13 | 0.0381 | 0.0305 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,446.79 ns | 2,967.622 ns | 162.665 ns | 43.37 |    1.77 | 0.0381 | 0.0305 |     712 B |        4.94 |
