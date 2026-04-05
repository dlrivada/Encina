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
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    12,656.8 ns |   577.64 ns | 343.75 ns |  1.00 |    0.04 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    21,304.4 ns | 1,362.92 ns | 901.49 ns |  1.68 |    0.08 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    24,809.3 ns |   336.27 ns | 222.42 ns |  1.96 |    0.05 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    12,761.3 ns |   275.21 ns | 143.94 ns |  1.01 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       833.8 ns |     5.05 ns |   2.64 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     7,881.0 ns |    94.70 ns |  62.64 ns |  0.62 |    0.02 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    33,665.0 ns |   199.77 ns | 132.14 ns |  2.66 |    0.07 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |                |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   402,013.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |      - |      - |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,703,436.0 ns |          NA |   0.00 ns |  6.72 |    0.00 |      - |      - |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,575,277.0 ns |          NA |   0.00 ns | 13.87 |    0.00 |      - |      - |   17096 B |        2.26 |
| &#39;Cache hit (memory)&#39;                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,188,765.0 ns |          NA |   0.00 ns |  7.93 |    0.00 |      - |      - |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,229,154.0 ns |          NA |   0.00 ns | 10.52 |    0.00 |      - |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,185,417.0 ns |          NA |   0.00 ns | 12.90 |    0.00 |      - |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,141,861.0 ns |          NA |   0.00 ns | 12.79 |    0.00 |      - |      - |    8592 B |        1.13 |
