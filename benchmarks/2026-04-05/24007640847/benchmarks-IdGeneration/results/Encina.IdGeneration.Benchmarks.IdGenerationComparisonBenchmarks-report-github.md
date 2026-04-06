```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.0 ns | 0.12 ns | 0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,107.2 ns | 9.71 ns | 6.42 ns |  4.57 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,087.1 ns | 1.63 ns | 1.07 ns |  4.49 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,410.3 ns | 4.92 ns | 2.93 ns |  5.83 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        605.8 ns | 2.15 ns | 1.28 ns |  2.50 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |         |      |        |           |             |
| Snowflake          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,080,489.0 ns |      NA | 0.00 ns |  1.00 |    0.00 |    2 |      - |         - |          NA |
| Ulid               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,298,502.0 ns |      NA | 0.00 ns |  1.09 |    0.00 |    4 |      - |      40 B |          NA |
| UuidV7             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,050,068.0 ns |      NA | 0.00 ns |  1.07 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,768,053.0 ns |      NA | 0.00 ns |  1.82 |    0.00 |    5 |      - |     216 B |          NA |
| DotNet_GuidNewGuid | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    317,803.0 ns |      NA | 0.00 ns |  0.02 |    0.00 |    1 |      - |         - |          NA |
