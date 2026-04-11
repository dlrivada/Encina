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
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   241.8 ns |  0.11 ns |  0.07 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 1,160.5 ns |  3.11 ns |  1.85 ns | 1,161.5 ns |  4.80 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 1,163.0 ns |  3.45 ns |  2.06 ns | 1,162.8 ns |  4.81 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,448.6 ns |  5.53 ns |  3.29 ns | 1,448.5 ns |  5.99 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   724.6 ns |  5.54 ns |  3.67 ns |   723.2 ns |  3.00 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   241.9 ns |  0.04 ns |  0.06 ns |   241.9 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 1,156.8 ns |  2.75 ns |  3.94 ns | 1,156.9 ns |  4.78 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 1,201.3 ns | 29.25 ns | 41.00 ns | 1,233.0 ns |  4.97 |    0.17 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,447.0 ns |  8.24 ns | 12.08 ns | 1,452.7 ns |  5.98 |    0.05 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   724.6 ns |  1.56 ns |  2.13 ns |   725.3 ns |  3.00 |    0.01 |    2 |      - |         - |          NA |
