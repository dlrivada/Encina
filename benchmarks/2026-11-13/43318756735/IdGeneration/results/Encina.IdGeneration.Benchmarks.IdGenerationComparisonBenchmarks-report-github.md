```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.09 ns |  0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 653.9 ns |   3.79 ns |  2.25 ns |  2.70 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 658.3 ns |   3.21 ns |  1.91 ns |  2.72 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 883.1 ns |  23.70 ns | 15.68 ns |  3.64 |    0.06 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 686.2 ns |   2.45 ns |  1.62 ns |  2.83 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.4 ns |   4.96 ns |  0.27 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 678.7 ns |  83.17 ns |  4.56 ns |  2.80 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 680.4 ns |   6.68 ns |  0.37 ns |  2.81 |    0.00 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 896.1 ns | 109.27 ns |  5.99 ns |  3.70 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 686.4 ns |   8.84 ns |  0.48 ns |  2.83 |    0.00 |    2 |      - |         - |          NA |
