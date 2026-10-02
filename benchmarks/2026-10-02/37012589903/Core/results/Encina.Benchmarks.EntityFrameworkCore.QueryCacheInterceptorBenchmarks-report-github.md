```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 12.717 μs |  0.2970 μs | 0.1767 μs |  1.00 |    0.02 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 24.221 μs |  2.6284 μs | 1.7385 μs |  1.90 |    0.13 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 21.960 μs |  0.3767 μs | 0.2492 μs |  1.73 |    0.03 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13.390 μs |  1.2897 μs | 0.8530 μs |  1.05 |    0.07 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |  1.042 μs |  0.0052 μs | 0.0027 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  7.844 μs |  0.0521 μs | 0.0310 μs |  0.62 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 34.970 μs |  0.3267 μs | 0.1944 μs |  2.75 |    0.04 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |           |            |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 12.719 μs |  1.7701 μs | 0.0970 μs |  1.00 |    0.01 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 21.576 μs |  1.7302 μs | 0.0948 μs |  1.70 |    0.01 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 20.505 μs |  0.6372 μs | 0.0349 μs |  1.61 |    0.01 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 13.262 μs | 15.0421 μs | 0.8245 μs |  1.04 |    0.06 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |  1.054 μs |  0.3857 μs | 0.0211 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8.003 μs |  0.1948 μs | 0.0107 μs |  0.63 |    0.00 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 35.959 μs |  2.5895 μs | 0.1419 μs |  2.83 |    0.02 | 0.4883 |      - |    8592 B |        1.13 |
