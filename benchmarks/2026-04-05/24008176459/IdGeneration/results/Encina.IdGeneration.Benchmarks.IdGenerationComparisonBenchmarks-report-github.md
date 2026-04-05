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
| Ulid               | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,188.2 ns | 7.06 ns | 4.67 ns |  4.91 |    0.02 |    4 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,086.6 ns | 2.50 ns | 1.65 ns |  4.49 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,355.7 ns | 5.29 ns | 3.50 ns |  5.60 |    0.01 |    5 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        605.1 ns | 1.69 ns | 1.12 ns |  2.50 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |             |              |             |                 |         |         |       |         |      |        |           |             |
| Snowflake          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,004,704.0 ns |      NA | 0.00 ns |  1.00 |    0.00 |    2 |      - |         - |          NA |
| Ulid               | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,377,501.0 ns |      NA | 0.00 ns |  1.11 |    0.00 |    4 |      - |      40 B |          NA |
| UuidV7             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,062,853.0 ns |      NA | 0.00 ns |  1.08 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,939,108.0 ns |      NA | 0.00 ns |  1.84 |    0.00 |    5 |      - |     216 B |          NA |
| DotNet_GuidNewGuid | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    332,021.0 ns |      NA | 0.00 ns |  0.03 |    0.00 |    1 |      - |         - |          NA |
