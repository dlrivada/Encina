```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  7,056.3 ns |   149.69 ns |  89.08 ns |  1.00 |    0.02 | 0.4501 | 0.1526 |    8088 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 14,760.9 ns |   643.33 ns | 336.48 ns |  2.09 |    0.05 | 0.8698 | 0.2899 |   16104 B |        1.99 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 11,835.8 ns |   183.89 ns | 109.43 ns |  1.68 |    0.02 | 0.9155 | 0.2289 |   16496 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  8,412.8 ns |   240.89 ns | 159.33 ns |  1.19 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.08 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    553.9 ns |     5.96 ns |   3.94 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,340.8 ns |    25.17 ns |  16.65 ns |  0.62 |    0.01 | 0.1602 |      - |    2712 B |        0.34 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 18,831.1 ns |   138.60 ns |  72.49 ns |  2.67 |    0.03 | 0.4883 |      - |    8592 B |        1.06 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  8,372.6 ns | 1,956.43 ns | 107.24 ns |  1.00 |    0.02 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 14,260.2 ns | 2,033.84 ns | 111.48 ns |  1.70 |    0.02 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 11,751.5 ns | 1,835.08 ns | 100.59 ns |  1.40 |    0.02 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  8,237.2 ns | 3,330.39 ns | 182.55 ns |  0.98 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    554.3 ns |     6.94 ns |   0.38 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,458.0 ns |   207.58 ns |  11.38 ns |  0.53 |    0.01 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 18,674.7 ns |   269.88 ns |  14.79 ns |  2.23 |    0.02 | 0.4883 |      - |    8592 B |        1.13 |
