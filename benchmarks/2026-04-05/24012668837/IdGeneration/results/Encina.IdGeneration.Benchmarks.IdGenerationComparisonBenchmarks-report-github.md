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
| Snowflake          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.1 ns | 0.07 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,092.3 ns | 4.73 ns | 3.13 ns |  4.51 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,079.8 ns | 3.79 ns | 2.25 ns |  4.46 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,337.3 ns | 6.24 ns | 4.12 ns |  5.52 |    0.02 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        605.4 ns | 1.06 ns | 0.70 ns |  2.50 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |         |      |        |           |             |
| Snowflake          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,678,590.0 ns |      NA | 0.00 ns |  1.00 |    0.00 |    2 |      - |         - |          NA |
| Ulid               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,773,527.0 ns |      NA | 0.00 ns |  1.09 |    0.00 |    4 |      - |      40 B |          NA |
| UuidV7             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,619,207.0 ns |      NA | 0.00 ns |  1.07 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,503,840.0 ns |      NA | 0.00 ns |  1.85 |    0.00 |    5 |      - |     216 B |          NA |
| DotNet_GuidNewGuid | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    354,806.0 ns |      NA | 0.00 ns |  0.03 |    0.00 |    1 |      - |         - |          NA |
