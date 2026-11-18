```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 241.3 ns |  0.12 ns | 0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 592.5 ns |  4.47 ns | 2.66 ns |  2.46 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 606.0 ns |  2.47 ns | 1.47 ns |  2.51 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 893.8 ns | 12.89 ns | 8.53 ns |  3.70 |    0.03 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 422.0 ns |  5.78 ns | 3.82 ns |  1.75 |    0.02 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 241.3 ns |  2.77 ns | 0.15 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 591.1 ns | 65.79 ns | 3.61 ns |  2.45 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 610.9 ns | 44.58 ns | 2.44 ns |  2.53 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 886.2 ns | 58.33 ns | 3.20 ns |  3.67 |    0.01 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 421.0 ns |  9.34 ns | 0.51 ns |  1.75 |    0.00 |    2 |      - |         - |          NA |
