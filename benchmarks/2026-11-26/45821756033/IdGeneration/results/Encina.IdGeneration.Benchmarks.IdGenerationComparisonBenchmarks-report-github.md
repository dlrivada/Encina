```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 241.2 ns |   0.07 ns |  0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 621.7 ns |   6.34 ns |  4.19 ns |  2.58 |    0.02 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 636.5 ns |   2.70 ns |  1.61 ns |  2.64 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 943.8 ns |  43.73 ns | 28.92 ns |  3.91 |    0.11 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 446.0 ns |   0.97 ns |  0.64 ns |  1.85 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.1 ns |  30.56 ns |  1.68 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 624.1 ns |  57.90 ns |  3.17 ns |  2.58 |    0.02 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 630.3 ns |  33.71 ns |  1.85 ns |  2.60 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 939.5 ns | 353.93 ns | 19.40 ns |  3.88 |    0.07 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 445.1 ns |   5.78 ns |  0.32 ns |  1.84 |    0.01 |    2 |      - |         - |          NA |
