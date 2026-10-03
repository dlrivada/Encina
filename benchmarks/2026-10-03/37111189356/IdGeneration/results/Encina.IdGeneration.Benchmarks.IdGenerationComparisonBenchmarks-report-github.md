```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.7 ns |   0.11 ns |  0.07 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   825.1 ns |   4.21 ns |  2.79 ns |  3.41 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   847.2 ns |   3.82 ns |  2.53 ns |  3.51 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,123.9 ns |  11.00 ns |  6.54 ns |  4.65 |    0.03 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   386.6 ns |   4.76 ns |  2.49 ns |  1.60 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.7 ns |   2.91 ns |  0.16 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   826.9 ns |  46.22 ns |  2.53 ns |  3.42 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   839.8 ns |  12.65 ns |  0.69 ns |  3.47 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,120.2 ns | 788.04 ns | 43.20 ns |  4.63 |    0.15 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   382.3 ns |  11.04 ns |  0.61 ns |  1.58 |    0.00 |    2 |      - |         - |          NA |
