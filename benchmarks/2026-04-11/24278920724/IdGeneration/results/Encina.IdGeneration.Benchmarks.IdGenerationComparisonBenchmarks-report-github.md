```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   241.9 ns |  0.11 ns |  0.07 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 1,235.9 ns |  5.98 ns |  3.56 ns | 1,234.3 ns |  5.11 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 1,233.7 ns |  4.01 ns |  2.39 ns | 1,233.5 ns |  5.10 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,527.5 ns |  3.20 ns |  1.90 ns | 1,527.4 ns |  6.32 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   730.9 ns |  7.79 ns |  4.07 ns |   732.6 ns |  3.02 |    0.02 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   241.9 ns |  0.04 ns |  0.06 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 1,196.9 ns | 29.83 ns | 39.82 ns | 1,230.3 ns |  4.95 |    0.16 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 1,158.9 ns |  2.76 ns |  3.87 ns | 1,157.3 ns |  4.79 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,433.6 ns |  4.83 ns |  6.92 ns | 1,432.9 ns |  5.93 |    0.03 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   723.9 ns |  1.41 ns |  2.02 ns |   724.5 ns |  2.99 |    0.01 |    2 |      - |         - |          NA |
