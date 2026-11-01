```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 10,409.0 ns |   636.80 ns |   421.20 ns | 10,250.1 ns |  1.00 |    0.05 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 17,536.4 ns |   731.62 ns |   483.92 ns | 17,475.6 ns |  1.69 |    0.08 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 16,946.3 ns |   478.19 ns |   316.29 ns | 16,872.5 ns |  1.63 |    0.07 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 10,929.9 ns |   688.33 ns |   455.29 ns | 10,993.9 ns |  1.05 |    0.06 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    894.5 ns |     2.98 ns |     1.77 ns |    893.8 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  6,252.7 ns |    25.85 ns |    17.10 ns |  6,255.7 ns |  0.60 |    0.02 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 26,425.5 ns |    97.99 ns |    64.81 ns | 26,424.8 ns |  2.54 |    0.10 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |             |             |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 10,104.0 ns |   185.13 ns |   265.51 ns | 10,044.1 ns |  1.00 |    0.04 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 18,073.9 ns | 1,357.37 ns | 1,989.62 ns | 16,680.9 ns |  1.79 |    0.20 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 16,243.4 ns |   376.32 ns |   551.61 ns | 16,085.1 ns |  1.61 |    0.07 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 10,563.6 ns |   425.13 ns |   623.15 ns | 10,263.0 ns |  1.05 |    0.07 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    908.2 ns |     1.72 ns |     2.29 ns |    908.2 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  6,185.1 ns |    19.47 ns |    29.14 ns |  6,189.3 ns |  0.61 |    0.02 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 26,525.5 ns |    74.75 ns |   107.21 ns | 26,520.4 ns |  2.63 |    0.07 | 0.4883 |      - |    8592 B |        1.13 |
