```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.64 ns |     0.044 ns |   0.045 ns |  0.26 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.60 ns |     0.078 ns |   0.087 ns |  0.30 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   200.30 ns |     0.722 ns |   0.832 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   134.06 ns |     0.133 ns |   0.148 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   242.70 ns |     1.455 ns |   1.676 ns |  1.21 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,829.57 ns |   514.769 ns | 550.797 ns | 29.11 |    2.68 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3,451.37 ns |    18.057 ns |  19.321 ns | 17.23 |    0.12 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,344.17 ns |   639.945 ns | 684.734 ns | 21.69 |    3.33 | 0.0381 | 0.0305 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,523.01 ns |   206.036 ns | 211.584 ns | 22.58 |    1.03 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    53.67 ns |     0.264 ns |   0.014 ns |  0.29 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    60.47 ns |     3.531 ns |   0.194 ns |  0.32 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   186.49 ns |    23.789 ns |   1.304 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   128.42 ns |     3.292 ns |   0.180 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   252.87 ns |   142.512 ns |   7.812 ns |  1.36 |    0.04 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5,014.07 ns | 2,033.931 ns | 111.487 ns | 26.89 |    0.54 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,430.41 ns |   683.611 ns |  37.471 ns | 18.39 |    0.21 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 4,134.90 ns | 3,590.079 ns | 196.784 ns | 22.17 |    0.92 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 4,362.21 ns | 3,831.659 ns | 210.026 ns | 23.39 |    0.99 | 0.0420 | 0.0381 |     712 B |        4.94 |
