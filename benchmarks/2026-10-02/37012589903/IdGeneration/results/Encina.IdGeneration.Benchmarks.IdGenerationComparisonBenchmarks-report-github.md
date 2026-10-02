```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.1 ns |   0.09 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,093.1 ns |   8.77 ns |  5.80 ns |  4.52 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,157.7 ns |   2.80 ns |  1.67 ns |  4.78 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,357.1 ns |   4.74 ns |  3.13 ns |  5.61 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   607.8 ns |   0.35 ns |  0.23 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.1 ns |   1.12 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,191.7 ns |  78.13 ns |  4.28 ns |  4.92 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,082.8 ns |  42.93 ns |  2.35 ns |  4.47 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,362.1 ns | 203.08 ns | 11.13 ns |  5.63 |    0.04 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   607.2 ns |  15.56 ns |  0.85 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
