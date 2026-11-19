```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 14.047 μs | 0.7628 μs | 0.5046 μs |  1.00 |    0.05 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 22.735 μs | 0.9957 μs | 0.6586 μs |  1.62 |    0.07 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 26.937 μs | 0.5329 μs | 0.3525 μs |  1.92 |    0.07 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13.548 μs | 0.3008 μs | 0.1574 μs |  0.97 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |  1.017 μs | 0.0017 μs | 0.0010 μs |  0.07 |    0.00 | 0.0057 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  8.062 μs | 0.0297 μs | 0.0155 μs |  0.57 |    0.02 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33.470 μs | 0.0966 μs | 0.0505 μs |  2.39 |    0.08 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |           |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13.340 μs | 3.7519 μs | 0.2057 μs |  1.00 |    0.02 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 23.086 μs | 6.2904 μs | 0.3448 μs |  1.73 |    0.03 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 27.232 μs | 5.3690 μs | 0.2943 μs |  2.04 |    0.03 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 14.084 μs | 2.9541 μs | 0.1619 μs |  1.06 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |  1.001 μs | 0.0747 μs | 0.0041 μs |  0.08 |    0.00 | 0.0057 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  7.993 μs | 2.8763 μs | 0.1577 μs |  0.60 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 33.251 μs | 2.1047 μs | 0.1154 μs |  2.49 |    0.03 | 0.4883 |      - |    8592 B |        1.13 |
