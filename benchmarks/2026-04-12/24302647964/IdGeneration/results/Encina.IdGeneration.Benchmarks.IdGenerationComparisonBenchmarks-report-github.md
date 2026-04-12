```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   241.9 ns |  0.08 ns |  0.05 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 1,242.8 ns | 11.09 ns |  7.34 ns | 1,240.6 ns |  5.14 |    0.03 |    4 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 1,147.9 ns |  2.71 ns |  1.79 ns | 1,147.8 ns |  4.75 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,439.4 ns |  3.91 ns |  2.04 ns | 1,440.1 ns |  5.95 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   724.6 ns |  7.28 ns |  4.33 ns |   722.2 ns |  3.00 |    0.02 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   241.9 ns |  0.03 ns |  0.04 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 1,153.9 ns |  1.88 ns |  2.63 ns | 1,153.5 ns |  4.77 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 1,245.2 ns |  3.61 ns |  5.18 ns | 1,246.0 ns |  5.15 |    0.02 |    4 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,450.2 ns | 17.61 ns | 24.68 ns | 1,433.0 ns |  6.00 |    0.10 |    5 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   725.0 ns |  3.61 ns |  4.94 ns |   722.3 ns |  3.00 |    0.02 |    2 |      - |         - |          NA |
