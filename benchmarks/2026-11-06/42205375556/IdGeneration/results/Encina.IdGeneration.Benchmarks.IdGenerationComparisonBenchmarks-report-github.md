```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.6 ns |   0.06 ns |  0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 501.1 ns |  13.89 ns |  9.19 ns |  2.07 |    0.04 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 512.8 ns |  13.70 ns |  9.06 ns |  2.11 |    0.04 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 678.2 ns |  31.91 ns | 21.11 ns |  2.80 |    0.08 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 454.0 ns |  14.83 ns |  8.83 ns |  1.87 |    0.03 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.6 ns |   1.44 ns |  0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 505.6 ns |  42.68 ns |  2.34 ns |  2.08 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 524.8 ns | 196.07 ns | 10.75 ns |  2.16 |    0.04 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 712.8 ns | 658.70 ns | 36.11 ns |  2.94 |    0.13 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 447.5 ns |  67.20 ns |  3.68 ns |  1.84 |    0.01 |    2 |      - |         - |          NA |
