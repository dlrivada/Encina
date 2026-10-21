```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 12,883.0 ns |   139.45 ns |  92.24 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 21,106.2 ns |   254.78 ns | 168.52 ns |  1.64 |    0.02 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 25,652.0 ns |   259.19 ns | 154.24 ns |  1.99 |    0.02 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13,115.2 ns |    99.30 ns |  65.68 ns |  1.02 |    0.01 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    954.9 ns |     1.55 ns |   0.92 ns |  0.07 |    0.00 | 0.0057 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  7,980.0 ns |    45.80 ns |  30.29 ns |  0.62 |    0.00 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33,773.6 ns |   169.11 ns | 111.85 ns |  2.62 |    0.02 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 12,334.5 ns |   843.37 ns |  46.23 ns |  1.00 |    0.00 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 20,993.3 ns | 2,522.73 ns | 138.28 ns |  1.70 |    0.01 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 25,189.0 ns | 2,257.75 ns | 123.75 ns |  2.04 |    0.01 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 13,646.3 ns |   930.55 ns |  51.01 ns |  1.11 |    0.01 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    973.7 ns |    11.73 ns |   0.64 ns |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8,038.7 ns |   473.93 ns |  25.98 ns |  0.65 |    0.00 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 33,719.5 ns | 1,175.26 ns |  64.42 ns |  2.73 |    0.01 | 0.4883 |      - |    8592 B |        1.13 |
