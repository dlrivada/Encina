```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 13.257 μs | 0.5157 μs | 0.3411 μs |  1.00 |    0.03 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 22.195 μs | 0.6210 μs | 0.4108 μs |  1.68 |    0.05 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 27.707 μs | 0.6342 μs | 0.4195 μs |  2.09 |    0.06 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13.590 μs | 0.3960 μs | 0.2619 μs |  1.03 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |  1.013 μs | 0.0022 μs | 0.0015 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  8.134 μs | 0.0460 μs | 0.0304 μs |  0.61 |    0.02 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33.839 μs | 0.0752 μs | 0.0497 μs |  2.55 |    0.06 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |           |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13.200 μs | 3.5378 μs | 0.1939 μs |  1.00 |    0.02 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 21.656 μs | 3.6710 μs | 0.2012 μs |  1.64 |    0.02 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 27.368 μs | 2.8707 μs | 0.1574 μs |  2.07 |    0.03 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 13.948 μs | 1.6495 μs | 0.0904 μs |  1.06 |    0.01 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |  1.009 μs | 0.0872 μs | 0.0048 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8.059 μs | 0.8390 μs | 0.0460 μs |  0.61 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 33.813 μs | 2.1072 μs | 0.1155 μs |  2.56 |    0.03 | 0.4883 |      - |    8592 B |        1.13 |
