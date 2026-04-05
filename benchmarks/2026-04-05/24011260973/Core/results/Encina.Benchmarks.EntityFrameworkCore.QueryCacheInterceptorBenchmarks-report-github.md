```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean           | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |---------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    12,585.2 ns | 134.38 ns |  79.97 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    20,449.4 ns | 220.85 ns | 146.08 ns |  1.62 |    0.01 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    25,674.0 ns | 127.05 ns |  75.60 ns |  2.04 |    0.01 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    12,811.9 ns | 192.13 ns | 127.08 ns |  1.02 |    0.01 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       830.8 ns |   2.90 ns |   1.92 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     7,988.7 ns |  43.82 ns |  28.98 ns |  0.63 |    0.00 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    33,279.0 ns |  74.06 ns |  38.74 ns |  2.64 |    0.02 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |                |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   384,804.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |      - |      - |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,623,214.0 ns |        NA |   0.00 ns |  6.82 |    0.00 |      - |      - |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,675,304.0 ns |        NA |   0.00 ns | 14.75 |    0.00 |      - |      - |   17096 B |        2.26 |
| &#39;Cache hit (memory)&#39;                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,187,151.0 ns |        NA |   0.00 ns |  8.28 |    0.00 |      - |      - |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,125,425.0 ns |        NA |   0.00 ns | 10.72 |    0.00 |      - |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,212,161.0 ns |        NA |   0.00 ns | 13.54 |    0.00 |      - |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,013,292.0 ns |        NA |   0.00 ns | 13.03 |    0.00 |      - |      - |    8592 B |        1.13 |
