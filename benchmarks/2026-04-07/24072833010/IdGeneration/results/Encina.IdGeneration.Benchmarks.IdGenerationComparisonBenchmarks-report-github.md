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
| Snowflake          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.1 ns | 0.08 ns | 0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,174.2 ns | 7.22 ns | 4.78 ns |  4.85 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,080.2 ns | 1.03 ns | 0.61 ns |  4.46 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,403.6 ns | 4.30 ns | 2.85 ns |  5.80 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        603.8 ns | 2.70 ns | 1.41 ns |  2.49 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |         |      |        |           |             |
| Snowflake          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,765,967.0 ns |      NA | 0.00 ns |  1.00 |    0.00 |    2 |      - |         - |          NA |
| Ulid               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,173,544.0 ns |      NA | 0.00 ns |  1.11 |    0.00 |    4 |      - |      40 B |          NA |
| UuidV7             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,921,552.0 ns |      NA | 0.00 ns |  1.09 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,113,064.0 ns |      NA | 0.00 ns |  1.81 |    0.00 |    5 |      - |     216 B |          NA |
| DotNet_GuidNewGuid | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    361,747.0 ns |      NA | 0.00 ns |  0.03 |    0.00 |    1 |      - |         - |          NA |
