```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  9,568.3 ns |   160.91 ns | 106.43 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 16,137.0 ns |   260.52 ns | 172.32 ns |  1.69 |    0.02 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 16,150.6 ns |   204.43 ns | 135.22 ns |  1.69 |    0.02 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  9,891.4 ns |   125.59 ns |  74.73 ns |  1.03 |    0.01 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    923.3 ns |    10.88 ns |   6.47 ns |  0.10 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  6,094.9 ns |     8.67 ns |   5.73 ns |  0.64 |    0.01 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 26,302.1 ns |    28.91 ns |  17.21 ns |  2.75 |    0.03 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  9,685.8 ns | 1,979.80 ns | 108.52 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 16,429.6 ns | 1,241.05 ns |  68.03 ns |  1.70 |    0.02 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 16,022.1 ns | 3,371.66 ns | 184.81 ns |  1.65 |    0.02 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  9,968.7 ns | 2,615.72 ns | 143.38 ns |  1.03 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    931.8 ns |   112.49 ns |   6.17 ns |  0.10 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  6,175.0 ns |    61.85 ns |   3.39 ns |  0.64 |    0.01 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 26,231.9 ns |   642.28 ns |  35.21 ns |  2.71 |    0.03 | 0.4883 |      - |    8592 B |        1.13 |
