```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.7 ns |   0.09 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 539.7 ns |  18.26 ns | 12.08 ns |  2.22 |    0.05 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 532.3 ns |  20.88 ns | 13.81 ns |  2.19 |    0.05 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 697.7 ns |  13.44 ns |  7.03 ns |  2.87 |    0.03 |    4 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 441.6 ns |   1.23 ns |  0.73 ns |  1.82 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.6 ns |   2.22 ns |  0.12 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 524.1 ns | 132.71 ns |  7.27 ns |  2.16 |    0.03 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 532.2 ns | 337.65 ns | 18.51 ns |  2.19 |    0.07 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 702.4 ns | 148.17 ns |  8.12 ns |  2.90 |    0.03 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 450.7 ns |  20.40 ns |  1.12 ns |  1.86 |    0.00 |    2 |      - |         - |          NA |
