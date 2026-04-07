```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.0 ns |   0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,107.3 ns |   5.32 ns | 3.52 ns |  4.58 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,097.3 ns |   2.36 ns | 1.23 ns |  4.53 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,396.7 ns |   2.75 ns | 1.44 ns |  5.77 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   604.4 ns |   1.41 ns | 0.93 ns |  2.50 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.3 ns |   7.57 ns | 0.41 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,098.8 ns | 115.96 ns | 6.36 ns |  4.54 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,166.1 ns |  97.49 ns | 5.34 ns |  4.81 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,322.2 ns |  59.15 ns | 3.24 ns |  5.46 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   605.0 ns |   2.99 ns | 0.16 ns |  2.50 |    0.00 |    2 |      - |         - |          NA |
