```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.3 ns |   0.10 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   969.4 ns |   4.19 ns |  2.49 ns |  4.02 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   989.2 ns |   4.50 ns |  2.68 ns |  4.10 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,273.8 ns |  15.26 ns | 10.09 ns |  5.28 |    0.04 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   444.8 ns |   1.04 ns |  0.69 ns |  1.84 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.4 ns |   1.39 ns |  0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   972.6 ns | 128.60 ns |  7.05 ns |  4.03 |    0.03 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   990.1 ns |  80.94 ns |  4.44 ns |  4.10 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,276.5 ns | 283.21 ns | 15.52 ns |  5.29 |    0.06 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   445.5 ns |  35.93 ns |  1.97 ns |  1.85 |    0.01 |    2 |      - |         - |          NA |
