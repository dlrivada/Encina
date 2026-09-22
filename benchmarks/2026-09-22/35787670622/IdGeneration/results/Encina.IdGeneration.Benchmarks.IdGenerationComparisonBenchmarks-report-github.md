```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.1 ns |   0.09 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,174.0 ns |   4.66 ns | 2.77 ns |  4.85 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,175.6 ns |   5.22 ns | 3.46 ns |  4.86 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,366.6 ns |   2.15 ns | 1.28 ns |  5.65 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   608.5 ns |   0.39 ns | 0.23 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.0 ns |   1.59 ns | 0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,096.9 ns |  91.93 ns | 5.04 ns |  4.53 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,102.8 ns |  44.22 ns | 2.42 ns |  4.56 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,367.3 ns | 131.46 ns | 7.21 ns |  5.65 |    0.03 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   608.2 ns |  10.87 ns | 0.60 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
