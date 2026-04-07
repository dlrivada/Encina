```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean           | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |---------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    13,203.5 ns |   404.70 ns | 267.68 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    21,860.2 ns |   584.04 ns | 347.55 ns |  1.66 |    0.04 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    27,169.6 ns |   628.21 ns | 415.52 ns |  2.06 |    0.05 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    14,612.9 ns | 1,232.84 ns | 815.45 ns |  1.11 |    0.06 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       846.1 ns |     4.97 ns |   2.96 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     8,050.8 ns |    28.85 ns |  15.09 ns |  0.61 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    33,873.7 ns |   145.93 ns |  86.84 ns |  2.57 |    0.05 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |                |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   427,521.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |      - |      - |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,772,133.0 ns |          NA |   0.00 ns |  6.48 |    0.00 |      - |      - |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,800,939.0 ns |          NA |   0.00 ns | 13.57 |    0.00 |      - |      - |   17096 B |        2.26 |
| &#39;Cache hit (memory)&#39;                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,329,067.0 ns |          NA |   0.00 ns |  7.79 |    0.00 |      - |      - |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,177,827.0 ns |          NA |   0.00 ns |  9.77 |    0.00 |      - |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,307,915.0 ns |          NA |   0.00 ns | 12.42 |    0.00 |      - |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,116,666.0 ns |          NA |   0.00 ns | 11.97 |    0.00 |      - |      - |    8592 B |        1.13 |
