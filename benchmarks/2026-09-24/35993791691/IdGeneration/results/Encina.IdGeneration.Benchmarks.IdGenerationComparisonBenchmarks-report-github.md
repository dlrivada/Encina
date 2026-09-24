```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.0 ns |  0.09 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,090.7 ns |  9.90 ns | 6.55 ns |  4.51 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,166.8 ns |  3.11 ns | 2.06 ns |  4.82 |    0.01 |    4 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,453.3 ns |  3.60 ns | 1.88 ns |  6.00 |    0.01 |    5 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   608.4 ns |  0.51 ns | 0.30 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.5 ns | 15.35 ns | 0.84 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,110.1 ns | 65.86 ns | 3.61 ns |  4.58 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,185.4 ns | 67.96 ns | 3.73 ns |  4.89 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,359.2 ns | 40.43 ns | 2.22 ns |  5.61 |    0.02 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   608.0 ns | 22.54 ns | 1.24 ns |  2.51 |    0.01 |    2 |      - |         - |          NA |
