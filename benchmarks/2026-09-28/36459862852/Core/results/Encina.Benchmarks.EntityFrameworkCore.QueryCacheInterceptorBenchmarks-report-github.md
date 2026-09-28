```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 13.062 μs | 0.1749 μs | 0.1041 μs |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 22.166 μs | 0.3358 μs | 0.2221 μs |  1.70 |    0.02 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 21.336 μs | 0.2841 μs | 0.1691 μs |  1.63 |    0.02 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13.661 μs | 0.3567 μs | 0.2359 μs |  1.05 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |  1.033 μs | 0.0033 μs | 0.0017 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  7.826 μs | 0.0427 μs | 0.0282 μs |  0.60 |    0.00 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33.597 μs | 0.0903 μs | 0.0472 μs |  2.57 |    0.02 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |           |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13.510 μs | 3.2950 μs | 0.1806 μs |  1.00 |    0.02 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 22.680 μs | 5.7428 μs | 0.3148 μs |  1.68 |    0.03 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 21.923 μs | 1.5360 μs | 0.0842 μs |  1.62 |    0.02 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 13.801 μs | 5.9850 μs | 0.3281 μs |  1.02 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |  1.036 μs | 0.0534 μs | 0.0029 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8.097 μs | 0.3724 μs | 0.0204 μs |  0.60 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 35.260 μs | 1.4596 μs | 0.0800 μs |  2.61 |    0.03 | 0.4883 |      - |    8592 B |        1.13 |
