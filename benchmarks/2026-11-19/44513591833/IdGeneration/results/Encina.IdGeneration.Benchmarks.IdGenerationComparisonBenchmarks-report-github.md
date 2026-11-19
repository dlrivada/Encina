```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.6 ns |   0.09 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 495.5 ns |  12.80 ns | 8.47 ns |  2.04 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 495.3 ns |  10.57 ns | 6.99 ns |  2.04 |    0.03 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 669.4 ns |  11.45 ns | 7.57 ns |  2.76 |    0.03 |    4 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 438.3 ns |   9.84 ns | 6.51 ns |  1.81 |    0.03 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.6 ns |   1.15 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 501.8 ns |  72.96 ns | 4.00 ns |  2.07 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 482.4 ns | 135.43 ns | 7.42 ns |  1.99 |    0.03 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 682.0 ns | 157.78 ns | 8.65 ns |  2.81 |    0.03 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 438.6 ns |  50.04 ns | 2.74 ns |  1.81 |    0.01 |    2 |      - |         - |          NA |
