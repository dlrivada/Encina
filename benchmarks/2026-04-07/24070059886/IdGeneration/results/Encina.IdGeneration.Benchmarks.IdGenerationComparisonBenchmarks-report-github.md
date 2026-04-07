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
| Snowflake          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.1 ns | 0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,096.5 ns | 8.95 ns | 5.92 ns |  4.53 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,081.1 ns | 2.54 ns | 1.51 ns |  4.47 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,331.1 ns | 5.47 ns | 3.62 ns |  5.50 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        612.1 ns | 1.52 ns | 0.90 ns |  2.53 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |         |      |        |           |             |
| Snowflake          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,497,809.0 ns |      NA | 0.00 ns |  1.00 |    0.00 |    2 |      - |         - |          NA |
| Ulid               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,737,637.0 ns |      NA | 0.00 ns |  1.10 |    0.00 |    3 |      - |      40 B |          NA |
| UuidV7             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,924,946.0 ns |      NA | 0.00 ns |  1.11 |    0.00 |    4 |      - |         - |          NA |
| ShardPrefixed      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,528,804.0 ns |      NA | 0.00 ns |  1.80 |    0.00 |    5 |      - |     216 B |          NA |
| DotNet_GuidNewGuid | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    375,780.0 ns |      NA | 0.00 ns |  0.03 |    0.00 |    1 |      - |         - |          NA |
